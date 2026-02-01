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
