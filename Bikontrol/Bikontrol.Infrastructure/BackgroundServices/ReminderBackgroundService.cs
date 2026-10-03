using Bikontrol.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bikontrol.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Runs the reminder engine once a day at <c>Reminders:DailyHourUtc</c>
    /// (default 08:00 UTC). PR1 only records due reminders (deduped); the email
    /// and push delivery read those records in later steps, so the engine stays
    /// side-effect-free for users.
    /// </summary>
    public class ReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReminderBackgroundService> _logger;

        private DateTime _lastRunUtc = DateTime.MinValue;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<ReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private bool IsEnabled =>
            !bool.TryParse(_configuration["Reminders:Enabled"], out var enabled) || enabled;

        private int DailyHourUtc =>
            int.TryParse(_configuration["Reminders:DailyHourUtc"], out var hour) && hour is >= 0 and <= 23
                ? hour
                : 8;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!IsEnabled)
            {
                _logger.LogInformation("Reminder engine disabled (Reminders:Enabled=false).");
                return;
            }

            _logger.LogInformation(
                "Reminder engine started; daily run at {Hour}:00 UTC.",
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
                    _logger.LogError(ex, "Reminder run failed: {Message}", ex.Message);
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

        /// <summary>Whether the digest email step runs (default true).</summary>
        private bool IsEmailEnabled =>
            !bool.TryParse(_configuration["Reminders:EmailEnabled"], out var enabled) || enabled;

        /// <summary>Whether the Web Push step runs (default true; a no-op without VAPID keys).</summary>
        private bool IsPushEnabled =>
            !bool.TryParse(_configuration["Reminders:PushEnabled"], out var enabled) || enabled;

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            _lastRunUtc = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IReminderService>();

            var generated = await service.GenerateDueRemindersAsync(cancellationToken);

            var emailed = 0;
            if (IsEmailEnabled)
            {
                emailed = await service.SendPendingEmailsAsync(cancellationToken);
            }

            var pushed = 0;
            if (IsPushEnabled)
            {
                pushed = await service.SendPendingPushesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Reminder run complete: {Generated} generated, {Emailed} digest email(s), {Pushed} push delivery(ies).",
                generated,
                emailed,
                pushed);
        }
    }
}
