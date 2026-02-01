using System.Collections.Generic;
using System.Threading.Tasks;
using MyErpApp.Core.Domain;

namespace MyErpApp.Core.Abstractions
{
    public interface IUiPageRepository
    {
        Task<UiPage?> GetByNameAsync(string name);
        Task<IEnumerable<UiPage>> GetAllAsync();
        Task AddAsync(UiPage page);
        Task UpdateAsync(UiPage page);
        Task DeleteAsync(string name);
    }
}
