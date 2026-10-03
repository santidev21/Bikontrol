using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IReminderLogRepository
    {
        /// <summary>
        /// True when the maintenance already produced a reminder of this kind
        /// since <paramref name="since"/> (dedupe window).
        /// </summary>
        Task<bool> ExistsSinceAsync(Guid userMaintenanceId, string kind, DateTime since);

        /// <summary>
        /// Maintenance ids that already produced a reminder of this kind since
        /// <paramref name="since"/>, to filter a batch without N queries.
        /// </summary>
        Task<IReadOnlySet<Guid>> GetRecentUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds, string kind, DateTime since);

        Task AddAsync(ReminderLog entity);

        /// <summary>
        /// Pending reminders (not yet delivered) with their user and maintenance
        /// loaded, so the digest can be built without N+1 queries.
        /// </summary>
        Task<IReadOnlyList<ReminderLog>> GetPendingWithDetailsAsync(string channel);

        /// <summary>Marks the given reminders as delivered on a channel.</summary>
        Task MarkDeliveredAsync(IEnumerable<Guid> ids, string channel, DateTime deliveredAt);

        Task SaveChangesAsync();
    }
}
