using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MyErpApp.Core.Plugins
{
    public interface IErpModule
    {
        string Name { get; }
        void RegisterServices(IServiceCollection services);
        void MapEndpoints(IEndpointRouteBuilder app);

        // New in Sprint 3.2
        string GetConfigurationSection() => $"Plugins:{Name}";
        void ValidateConfiguration(Microsoft.Extensions.Configuration.IConfiguration config) { }
        bool AllowServiceOverride => false;

        // New in Sprint 4.1
        int MigrationPriority => 100;
        IEnumerable<string> DependsOnModules => Array.Empty<string>();
        Task ApplyMigrations(IServiceProvider serviceProvider) => Task.CompletedTask;
    }

    public interface IUiComponentPlugin
    {
        string Type { get; }
        string DisplayName { get; }
        string DefaultConfig();
        string RenderHtml(string config);
    }

    public interface IUiRenderExtension
    {
        string Name { get; }
        string RenderPage(string html, string configJson);
    }
}
