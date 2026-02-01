using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyErpApp.Core.Abstractions;

namespace MyErpApp.UiRuntimeCache
{
    public class UiCachePreloader : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UiCachePreloader> _logger;
        private readonly IUiRenderCacheService _cache;

        public UiCachePreloader(IServiceProvider serviceProvider, ILogger<UiCachePreloader> logger, IUiRenderCacheService cache)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cache = cache;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UI Cache Preloader starting...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IUiPageRepository>();
                var renderer = scope.ServiceProvider.GetRequiredService<IUiPageRenderer>();

                var pages = await repository.GetAllAsync();
                _logger.LogInformation("Preloading {Count} pages into cache.", pages.Count());

                foreach (var page in pages)
                {
                    try
                    {
                        var html = await renderer.RenderPageAsync(page);
                        _cache.Refresh(page.Name, html);
                        _logger.LogInformation("Cached page: {PageName}", page.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cache page: {PageName}", page.Name);
                    }
                }
            }

            _logger.LogInformation("UI Cache Preloader completed initial warming.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
