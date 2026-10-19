using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Retention
{
    /// <summary>Row counts removed by one retention purge run.</summary>
    public class RetentionRunResult
    {
        /// <summary>Expired or revoked refresh tokens deleted.</summary>
        public int RefreshTokensDeleted { get; set; }

        /// <summary>Reminder history entries deleted.</summary>
        public int ReminderLogsDeleted { get; set; }

        /// <summary>Audit entries deleted (0 while audit retention is off).</summary>
        public int AuditLogsDeleted { get; set; }
    }
}
