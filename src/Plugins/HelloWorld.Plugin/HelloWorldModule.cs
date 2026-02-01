using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MyErpApp.Core.Plugins;
using System.Composition;

namespace HelloWorld.Plugin
{
    [Export(typeof(IErpModule))]
    public class HelloWorldModule : IErpModule
    {
        public string Name => "HelloWorld";

        public void RegisterServices(IServiceCollection services)
        {
            // Register plugin-specific services here
        }

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("/hello", () => "Hello from World Plugin!");
        }
    }
}
