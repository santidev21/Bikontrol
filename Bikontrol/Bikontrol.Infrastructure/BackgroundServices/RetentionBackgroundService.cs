using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bikontrol.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Runs the data-retention purge once a day at <c>Retention:DailyHourUtc</c>
    /// (default 04:00 UTC — off-peak and clear of the reminder engine's 08:00).
    /// Mirrors ReminderBackgroundService's scheduler semantics: a bad run is
    /// logged and retried on the next tick instead of killing the scheduler.
    /// </summary>
    public class RetentionBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RetentionOptions _options;
        private readonly ILogger<RetentionBackgroundService> _logger;

        private DateTime _lastRunUtc = DateTime.MinValue;

        public RetentionBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<RetentionOptions> options,
            ILogger<RetentionBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        private bool IsEnabled => _options.Enabled;

        private int DailyHourUtc => _options.DailyHourUtc;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!IsEnabled)
            {
                _logger.LogInformation("Retention purge disabled (Retention:Enabled=false).");
                return;
            }

            _logger.LogInformation(
                "Retention purge scheduled; daily run at {Hour}:00 UTC.",
                DailyHourUtc);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (ShouldRunNow(DateTime.UtcNow))
                    {
                        await RunOnceAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Never let a bad run kill the scheduler; retry on the next tick.
                    _logger.LogError(ex, "Retention run failed: {Message}", ex.Message);
                }

                await Task.Delay(Tick, stoppingToken);
            }
        }

        private bool ShouldRunNow(DateTime nowUtc)
        {
            if (nowUtc.Hour < DailyHourUtc)
                return false;
            return _lastRunUtc == DateTime.MinValue || _lastRunUtc.Date < nowUtc.Date;
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            // One instance at a time: another replica holding the lock is already
            // doing this run. Release happens when the handle is disposed.
            var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();
            await using var handle = await distributedLock.TryAcquireAsync("bikontrol:job:retention", cancellationToken);
            if (handle is null)
            {
                _logger.LogInformation("Retention run skipped: another instance holds the lock.");
                _lastRunUtc = DateTime.UtcNow;
                return;
            }

            var service = scope.ServiceProvider.GetRequiredService<IRetentionService>();

            var result = await service.PurgeAsync(cancellationToken);

            _logger.LogInformation(
                "Retention run complete: {RefreshTokens} refresh token(s), {ReminderLogs} reminder log(s), {AuditLogs} audit log(s) deleted.",
                result.RefreshTokensDeleted,
                result.ReminderLogsDeleted,
                result.AuditLogsDeleted);

            // Only a fully successful run counts: a failure retries on the next tick.
            _lastRunUtc = DateTime.UtcNow;
        }
    }
}
