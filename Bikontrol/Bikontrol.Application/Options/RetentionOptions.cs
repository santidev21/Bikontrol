using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>Data-retention purge job (<c>Retention</c> section).</summary>
    public class RetentionOptions
    {
        public const string Section = "Retention";

        public bool Enabled { get; set; } = true;

        [Range(0, 23)]
        public int DailyHourUtc { get; set; } = 4;

        /// <summary>Days a dead refresh token is kept after expiring or being revoked.</summary>
        [Range(1, 3650)]
        public int RefreshTokenDays { get; set; } = 30;

        /// <summary>Days reminder history is kept (the dedupe window needs a few).</summary>
        [Range(1, 3650)]
        public int ReminderLogDays { get; set; } = 90;

        /// <summary>Days audit entries are kept; 0 keeps them forever (append-only by design).</summary>
        [Range(0, 36500)]
        public int AuditLogDays { get; set; }
    }
}
