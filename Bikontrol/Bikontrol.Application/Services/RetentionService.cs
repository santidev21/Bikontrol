using Bikontrol.Application.DTOs.Retention;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Data-retention purge. Refresh tokens rotate on every session refresh
    /// (several rows per active user per day) and reminder logs accumulate on
    /// every engine run, so both tables grow without bound. Delegates to the
    /// repositories' bulk deletes, which bypass the change tracker and the
    /// audit trail (see AccountRepository for the same rationale).
    /// </summary>
    public class RetentionService : IRetentionService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IReminderLogRepository _reminderLogRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly RetentionOptions _options;

        public RetentionService(
            IRefreshTokenRepository refreshTokenRepository,
            IReminderLogRepository reminderLogRepository,
            IAuditLogRepository auditLogRepository,
            IOptions<RetentionOptions> options)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _reminderLogRepository = reminderLogRepository;
            _auditLogRepository = auditLogRepository;
            _options = options.Value;
        }

        private int RefreshTokenRetentionDays => _options.RefreshTokenDays;

        private int ReminderLogRetentionDays => _options.ReminderLogDays;

        /// <summary>0 keeps audit entries forever (append-only by design).</summary>
        private int AuditLogRetentionDays => _options.AuditLogDays;

        public async Task<RetentionRunResult> PurgeAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var refreshTokensDeleted = await _refreshTokenRepository.DeleteInactiveBeforeAsync(
                now.AddDays(-RefreshTokenRetentionDays), cancellationToken);

            var reminderLogsDeleted = await _reminderLogRepository.DeleteOlderThanAsync(
                now.AddDays(-ReminderLogRetentionDays), cancellationToken);

            // Opt-in only: the audit trail is intentionally append-only.
            var auditLogsDeleted = AuditLogRetentionDays > 0
                ? await _auditLogRepository.DeleteOlderThanAsync(
                    now.AddDays(-AuditLogRetentionDays), cancellationToken)
                : 0;

            return new RetentionRunResult
            {
                RefreshTokensDeleted = refreshTokensDeleted,
                ReminderLogsDeleted = reminderLogsDeleted,
                AuditLogsDeleted = auditLogsDeleted
            };
        }
    }
}
