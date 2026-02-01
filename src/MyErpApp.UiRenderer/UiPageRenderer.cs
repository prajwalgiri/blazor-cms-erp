using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Domain;
using MyErpApp.Core.Plugins;

namespace MyErpApp.UiRenderer
{
    public class UiPageRenderer : IUiPageRenderer
    {
        private readonly IPluginHealthMonitor _healthMonitor;
        private readonly IEnumerable<IUiComponentPlugin> _componentPlugins;

        public UiPageRenderer(IPluginHealthMonitor healthMonitor, IEnumerable<IUiComponentPlugin> componentPlugins)
        {
            _healthMonitor = healthMonitor;
            _componentPlugins = componentPlugins;
        }

        public Task<string> RenderPageAsync(UiPage page)
        {
            var sb = new StringBuilder();
            sb.Append($"<div class=\"erp-page\" data-page-id=\"{page.Id}\">");
            sb.Append($"<h1 class=\"text-2xl font-bold mb-4\">{page.Title}</h1>");

            foreach (var component in page.Components.OrderBy(c => c.Order))
            {
                var plugin = _componentPlugins.FirstOrDefault(p => p.Type == component.Type);
                if (plugin != null)
                {
                    try
                    {
                        var html = plugin.RenderHtml(component.ConfigJson);
                        sb.Append(html);
                    }
                    catch (System.Exception ex)
                    {
                        sb.Append($"<div class=\"error\">Error rendering component {component.Id}: {ex.Message}</div>");
                        _healthMonitor.RecordFailure($"Component:{component.Type}", ex);
                    }
                }
                else
                {
                    sb.Append($"<div class=\"warning\">Component plugin not found: {component.Type}</div>");
                }
            }

            sb.Append("</div>");
            return Task.FromResult(sb.ToString());
        }
    }
}
