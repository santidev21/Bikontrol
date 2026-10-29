using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>Maintenance reminder job (<c>Reminders</c> section).</summary>
    public class ReminderOptions
    {
        public const string Section = "Reminders";

        public bool Enabled { get; set; } = true;

        [Range(0, 23)]
        public int DailyHourUtc { get; set; } = 8;

        public bool EmailEnabled { get; set; } = true;

        public bool PushEnabled { get; set; } = true;

        /// <summary>Don't remind the same item twice within this many days.</summary>
        [Range(1, 30)]
        public int DedupeDays { get; set; } = 3;
    }
}
