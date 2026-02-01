using System.Collections.Concurrent;
using System.Collections.Generic;
using MyErpApp.Core.Abstractions;

namespace MyErpApp.UiRuntimeCache
{
    public class UiRenderCacheService : IUiRenderCacheService
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public void Refresh(string pageName, string html)
        {
            _cache[pageName] = html;
        }

        public void RefreshAll(Dictionary<string, string> cachedPages)
        {
            _cache.Clear();
            foreach (var kvp in cachedPages)
            {
                _cache[kvp.Key] = kvp.Value;
            }
        }

        public string? GetHtml(string pageName)
        {
            return _cache.TryGetValue(pageName, out var html) ? html : null;
        }

        public void Invalidate(string pageName)
        {
            _cache.TryRemove(pageName, out _);
        }

        public void InvalidateAll()
        {
            _cache.Clear();
        }
    }
}
