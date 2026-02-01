using System.Collections.Generic;
using System.Threading.Tasks;
using MyErpApp.Core.Domain;

namespace MyErpApp.Core.Abstractions
{
    public interface IUiRenderCacheService
    {
        void Refresh(string pageName, string html);
        void RefreshAll(Dictionary<string, string> cachedPages);
        string? GetHtml(string pageName);
        void Invalidate(string pageName);
        void InvalidateAll();
    }
}
