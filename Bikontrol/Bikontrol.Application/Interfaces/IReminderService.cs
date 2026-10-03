using Bikontrol.Application.DTOs.Reminders;

namespace Bikontrol.Application.Interfaces
{
    public interface IReminderService
    {
        /// <summary>
        /// Maintenance that is due or overdue for the current user (the same
        /// countdown the dashboard shows), across all their motorcycles.
        /// </summary>
        Task<IReadOnlyList<DueReminderDTO>> GetDueForCurrentUserAsync();

        /// <summary>
        /// Runs the reminder engine for every eligible user: finds due items not
        /// already reminded within the dedupe window and records them as pending
        /// deliveries. Returns how many were generated. Called by the daily job.
        /// </summary>
        Task<int> GenerateDueRemindersAsync(CancellationToken cancellationToken = default);
    }
}
