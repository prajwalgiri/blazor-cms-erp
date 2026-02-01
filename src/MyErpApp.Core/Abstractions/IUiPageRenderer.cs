using System.Threading.Tasks;
using MyErpApp.Core.Domain;

namespace MyErpApp.Core.Abstractions
{
    public interface IUiPageRenderer
    {
        Task<string> RenderPageAsync(UiPage page);
    }
}
