using Bikontrol.Application.DTOs.Retention;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public RetentionService(
            IRefreshTokenRepository refreshTokenRepository,
            IReminderLogRepository reminderLogRepository,
            IAuditLogRepository auditLogRepository,
            IConfiguration configuration)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _reminderLogRepository = reminderLogRepository;
            _auditLogRepository = auditLogRepository;
            _configuration = configuration;
        }

        /// <summary>Days a dead token is kept after expiring or being revoked (default 30).</summary>
        private int RefreshTokenRetentionDays =>
            int.TryParse(_configuration["Retention:RefreshTokenDays"], out var days) && days > 0 ? days : 30;

        /// <summary>Days reminder history is kept (default 90; the dedupe window needs 3).</summary>
        private int ReminderLogRetentionDays =>
            int.TryParse(_configuration["Retention:ReminderLogDays"], out var days) && days > 0 ? days : 90;

        /// <summary>
        /// Days audit entries are kept. 0 (the default) keeps them forever:
        /// the audit trail is append-only by design until an explicit policy
        /// says otherwise.
        /// </summary>
        private int AuditLogRetentionDays =>
            int.TryParse(_configuration["Retention:AuditLogDays"], out var days) && days > 0 ? days : 0;

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
