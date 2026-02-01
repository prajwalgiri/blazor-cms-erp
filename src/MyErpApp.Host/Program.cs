using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Domain;
using MyErpApp.Core.Plugins;
using MyErpApp.Infrastructure.Data;
using MyErpApp.Infrastructure.Repositories;
using MyErpApp.Infrastructure.Services;
using MyErpApp.UiRenderer;
using MyErpApp.UiRuntimeCache;
using Microsoft.Extensions.Hosting;
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
builder.Services.AddSingleton<IUiRenderCacheService, UiRenderCacheService>();
builder.Services.AddScoped<IUiPageRenderer, UiPageRenderer>();
builder.Services.AddHostedService<UiCachePreloader>();

// Load Plugins via MEF
var pluginPath = Path.Combine(builder.Environment.ContentRootPath, "..", "plugins");
var (pluginHost, loadResults) = PluginLoader.LoadPlugins(pluginPath);
var healthMonitor = new PluginHealthMonitor();

foreach (var result in loadResults)
{
    healthMonitor.RecordLoadResult(result);
}

builder.Services.AddSingleton<IPluginHealthMonitor>(healthMonitor);

// Discover and register ERP Modules
var modules = pluginHost.GetExports<IErpModule>().ToList();

// Discover and register UI Component Plugins
var componentPlugins = pluginHost.GetExports<IUiComponentPlugin>().ToList();
foreach (var component in componentPlugins)
{
    builder.Services.AddSingleton<IUiComponentPlugin>(component);
}

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

    // Seed sample UI data
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.UiPages.Any(p => p.Name == "Showcase"))
    {
        var page = new UiPage { Name = "Showcase", Title = "Component Showcase" };
        
        page.Components.Add(new UiComponent 
        { 
            Type = "Heading", 
            Order = 1, 
            ConfigJson = "{\"Text\": \"UI Component Library\", \"Level\": \"1\"}" 
        });

        page.Components.Add(new UiComponent 
        { 
            Type = "Input", 
            Order = 2, 
            ConfigJson = "{\"Label\": \"Full Name\", \"Placeholder\": \"John Doe\"}" 
        });

        page.Components.Add(new UiComponent 
        { 
            Type = "Select", 
            Order = 3, 
            ConfigJson = "{\"Label\": \"Department\", \"Items\": \"it:IT,hr:HR,sales:Sales\"}" 
        });

        page.Components.Add(new UiComponent 
        { 
            Type = "Checkbox", 
            Order = 4, 
            ConfigJson = "{\"Label\": \"I accept the terms and conditions\", \"Checked\": \"true\"}" 
        });

        page.Components.Add(new UiComponent 
        { 
            Type = "Button", 
            Order = 5, 
            ConfigJson = "{\"Text\": \"Submit Request\"}" 
        });

        db.UiPages.Add(page);
        await db.SaveChangesAsync();
        Log.Information("Seeded Component Showcase page.");
    }
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
    LoadedModules = modules.Select(m => m.Name),
    LoadedComponents = componentPlugins.Select(c => c.DisplayName)
}));

app.MapGet("/admin/plugins/status", (IPluginHealthMonitor monitor) => 
    Results.Ok(monitor.GetStatus()));

app.MapGet("/ui/render/{pageName}", async (string pageName, IUiRenderCacheService cache) => 
{
    var html = cache.GetHtml(pageName);
    if (html == null) return Results.NotFound($"Page {pageName} not found in cache.");

    var fullHtml = $@"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <script src=""https://cdn.tailwindcss.com""></script>
            <title>{pageName} - MyErpApp</title>
        </head>
        <body class=""bg-gray-50 text-gray-900"">
            <div class=""max-w-4xl mx-auto p-8 bg-white shadow-lg mt-10 rounded-lg"">
                {html}
            </div>
        </body>
        </html>";

    return Results.Content(fullHtml, "text/html");
});

app.Run();
