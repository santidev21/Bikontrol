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

        /// <summary>
        /// Sends one digest email per user for their pending reminders and marks
        /// them delivered. Returns how many emails were sent. Called by the daily
        /// job right after <see cref="GenerateDueRemindersAsync"/>.
        /// </summary>
        Task<int> SendPendingEmailsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Delivers still-pending reminders by Web Push and marks them delivered.
        /// Returns how many users were notified. Called by the daily job after
        /// the email step.
        /// </summary>
        Task<int> SendPendingPushesAsync(CancellationToken cancellationToken = default);
    }
}
