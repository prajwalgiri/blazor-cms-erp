using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Plugins;
using MyErpApp.Infrastructure.Data;
using MyErpApp.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi();

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUiPageRepository, UiPageRepository>();

// Load Plugins via MEF
var pluginPath = Path.Combine(builder.Environment.ContentRootPath, "..", "plugins");
var pluginHost = PluginLoader.LoadPlugins(pluginPath);
var modules = pluginHost.GetExports<IErpModule>().ToList();

foreach (var module in modules)
{
    Log.Information("Registering services for module: {ModuleName}", module.Name);
    module.RegisterServices(builder.Services);
}

var app = builder.Build();

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
    Log.Information("Mapping endpoints for module: {ModuleName}", module.Name);
    module.MapEndpoints(app);
}

app.MapGet("/", () => "ERP-CMS Modular Monolith Host Running.");

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    LoadedModules = modules.Select(m => m.Name)
}));

app.Run();
