using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        /// <summary>Most recent audit entries authored by one user.</summary>
        Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int limit);

        /// <summary>
        /// Bulk-deletes audit entries older than the cutoff (retention purge;
        /// bypasses the change tracker). Returns how many rows were deleted.
        /// </summary>
        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

        Task SaveChangesAsync();
    }
}
