using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        /// <summary>Most recent audit entries authored by one user.</summary>
        Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int limit);

        Task SaveChangesAsync();
    }
}
