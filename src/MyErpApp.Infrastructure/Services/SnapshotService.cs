using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyErpApp.Core.Abstractions;
using MyErpApp.Core.Domain;
using MyErpApp.Infrastructure.Data;

namespace MyErpApp.Infrastructure.Services
{
    public class SnapshotService : ISnapshotService
    {
        private readonly AppDbContext _context;

        public SnapshotService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateSnapshotAsync<T>(Guid entityId, T entity) where T : class
        {
            var snapshot = new EntitySnapshot
            {
                Id = Guid.NewGuid(),
                EntityName = typeof(T).Name,
                EntityId = entityId,
                JsonData = JsonSerializer.Serialize(entity),
                SnapshotDate = DateTime.UtcNow
            };

            await _context.EntitySnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
        }

        public async Task<T?> RestoreFromSnapshotAsync<T>(Guid snapshotId) where T : class
        {
            var snapshot = await _context.EntitySnapshots.FindAsync(snapshotId);
            if (snapshot == null) return null;

            return JsonSerializer.Deserialize<T>(snapshot.JsonData);
        }

        public async Task<T?> GetLatestSnapshotAsync<T>(Guid entityId) where T : class
        {
            var snapshot = await _context.EntitySnapshots
                .Where(s => s.EntityId == entityId && s.EntityName == typeof(T).Name)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync();

            if (snapshot == null) return null;

            return JsonSerializer.Deserialize<T>(snapshot.JsonData);
        }
    }
}
