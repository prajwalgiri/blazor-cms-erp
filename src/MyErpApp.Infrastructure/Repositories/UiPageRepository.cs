using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Domain;
using MyErpApp.Infrastructure.Data;

namespace MyErpApp.Infrastructure.Repositories
{
    public class UiPageRepository : IUiPageRepository
    {
        private readonly AppDbContext _context;

        public UiPageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UiPage?> GetByNameAsync(string name)
        {
            return await _context.UiPages
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<IEnumerable<UiPage>> GetAllAsync()
        {
            return await _context.UiPages
                .Include(p => p.Components)
                .ToListAsync();
        }

        public async Task AddAsync(UiPage page)
        {
            await _context.UiPages.AddAsync(page);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UiPage page)
        {
            _context.UiPages.Update(page);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string name)
        {
            var page = await GetByNameAsync(name);
            if (page != null)
            {
                _context.UiPages.Remove(page);
                await _context.SaveChangesAsync();
            }
        }
    }
}
