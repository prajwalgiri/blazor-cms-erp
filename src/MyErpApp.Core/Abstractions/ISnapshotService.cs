using System;
using System.Threading.Tasks;

namespace MyErpApp.Core.Abstractions
{
    public interface ISnapshotService
    {
        Task CreateSnapshotAsync<T>(Guid entityId, T entity) where T : class;
        Task<T?> RestoreFromSnapshotAsync<T>(Guid snapshotId) where T : class;
        Task<T?> GetLatestSnapshotAsync<T>(Guid entityId) where T : class;
    }
}
