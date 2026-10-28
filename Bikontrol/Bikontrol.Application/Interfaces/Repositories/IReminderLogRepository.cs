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
        /// Reminders created since <paramref name="since"/> that were not yet
        /// delivered on <paramref name="channel"/> (<see cref="ReminderChannel.Email"/>
        /// or <see cref="ReminderChannel.Push"/>), with their user and maintenance
        /// loaded so the digest can be built without N+1 queries. Each channel
        /// tracks its own delivery, so one channel never starves the other.
        /// </summary>
        Task<IReadOnlyList<ReminderLog>> GetPendingWithDetailsAsync(string channel, DateTime since);

        /// <summary>Marks the given reminders as delivered on a channel.</summary>
        Task MarkDeliveredAsync(IEnumerable<Guid> ids, string channel, DateTime deliveredAt);

        /// <summary>
        /// Bulk-deletes reminder history older than the cutoff (retention
        /// purge; bypasses the change tracker and the audit trail).
        /// Returns how many rows were deleted.
        /// </summary>
        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

        Task SaveChangesAsync();
    }
}
