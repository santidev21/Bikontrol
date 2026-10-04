using Bikontrol.Application.DTOs.Audit;

namespace Bikontrol.Application.Interfaces
{
    public interface IAuditLogService
    {
        /// <summary>
        /// The current user's most recent activity, newest first. Read-only;
        /// the demo tenant can view its own (seeded) trail.
        /// </summary>
        Task<IReadOnlyList<AuditLogDTO>> GetMyActivityAsync(int limit = 50);
    }
}
