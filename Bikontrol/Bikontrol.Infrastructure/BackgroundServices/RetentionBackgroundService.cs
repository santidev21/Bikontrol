using Bikontrol.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        private readonly IConfiguration _configuration;
        private readonly ILogger<RetentionBackgroundService> _logger;

        private DateTime _lastRunUtc = DateTime.MinValue;

        public RetentionBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RetentionBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private bool IsEnabled =>
            !bool.TryParse(_configuration["Retention:Enabled"], out var enabled) || enabled;

        private int DailyHourUtc =>
            int.TryParse(_configuration["Retention:DailyHourUtc"], out var hour) && hour is >= 0 and <= 23
                ? hour
                : 4;

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
            _lastRunUtc = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRetentionService>();

            var result = await service.PurgeAsync(cancellationToken);

            _logger.LogInformation(
                "Retention run complete: {RefreshTokens} refresh token(s), {ReminderLogs} reminder log(s), {AuditLogs} audit log(s) deleted.",
                result.RefreshTokensDeleted,
                result.ReminderLogsDeleted,
                result.AuditLogsDeleted);
        }
    }
}
