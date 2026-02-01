using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Plugins;
using MyErpApp.Infrastructure.Data;
using MyErpApp.Infrastructure.Repositories;
using MyErpApp.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Plugin-specific configuration
builder.Configuration.AddJsonFile("appsettings.Plugins.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddOpenApi();

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUiPageRepository, UiPageRepository>();

// Register Services
builder.Services.AddScoped<ISnapshotService, SnapshotService>();
builder.Services.AddScoped<IMigrationCoordinator, MigrationCoordinator>();

// Load Plugins via MEF
var pluginPath = Path.Combine(builder.Environment.ContentRootPath, "..", "plugins");
var (pluginHost, loadResults) = PluginLoader.LoadPlugins(pluginPath);
var healthMonitor = new PluginHealthMonitor();

foreach (var result in loadResults)
{
    healthMonitor.RecordLoadResult(result);
}

builder.Services.AddSingleton<IPluginHealthMonitor>(healthMonitor);

var modules = pluginHost.GetExports<IErpModule>().ToList();

// Track services to detect conflicts
var registeredServices = new Dictionary<Type, string>();

foreach (var module in modules)
{
    try
    {
        Log.Information("Validating configuration for module: {ModuleName}", module.Name);
        module.ValidateConfiguration(builder.Configuration);

        Log.Information("Registering services for module: {ModuleName}", module.Name);

        // Temporary service collection to detect overlaps
        var tempServices = new ServiceCollection();
        module.RegisterServices(tempServices);

        foreach (var service in tempServices)
        {
            if (registeredServices.TryGetValue(service.ServiceType, out var otherModule))
            {
                if (!module.AllowServiceOverride)
                {
                    Log.Warning("DI CONFLICT: Module {ModuleName} is trying to register {ServiceType} which was already registered by {OtherModule}.",
                        module.Name, service.ServiceType.Name, otherModule);
                    continue; // Skip this service if override not allowed
                }
            }

            registeredServices[service.ServiceType] = module.Name;
            builder.Services.Add(service);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize module {ModuleName}", module.Name);
        healthMonitor.RecordFailure(module.Name, ex);
    }
}

var app = builder.Build();

// Coordinated Migrations
using (var scope = app.Services.CreateScope())
{
    var coordinator = scope.ServiceProvider.GetRequiredService<IMigrationCoordinator>();
    await coordinator.CoordinateMigrations(modules);
}

// Global Exception Handling
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception occurred.");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { Error = "An internal server error occurred.", Details = ex.Message });
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Map Module Endpoints
foreach (var module in modules)
{
    try
    {
        Log.Information("Mapping endpoints for module: {ModuleName}", module.Name);
        module.MapEndpoints(app);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to map endpoints for module {ModuleName}", module.Name);
        healthMonitor.RecordFailure(module.Name, ex);
    }
}

app.MapGet("/", () => "ERP-CMS Modular Monolith Host Running.");

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    LoadedModules = modules.Select(m => m.Name)
}));

app.MapGet("/admin/plugins/status", (IPluginHealthMonitor monitor) =>
    Results.Ok(monitor.GetStatus()));

app.Run();
