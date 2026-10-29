using Bikontrol.Application.Interfaces;
using System.Diagnostics;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private const string JobName = "reminders";

        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReminderOptions _options;
        private readonly ILogger<ReminderBackgroundService> _logger;

        private DateTime _lastRunUtc = DateTime.MinValue;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<ReminderOptions> options,
            ILogger<ReminderBackgroundService> logger)
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
        private bool IsEmailEnabled => _options.EmailEnabled;

        /// <summary>Whether the Web Push step runs (default true; a no-op without VAPID keys).</summary>
        private bool IsPushEnabled => _options.PushEnabled;

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = JobMetrics.Failed;
            try
            {
                result = await RunCoreAsync(cancellationToken);
            }
            finally
            {
                JobMetrics.RecordRun(JobName, result, stopwatch.Elapsed);
            }
        }

        private async Task<string> RunCoreAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            // One instance at a time: another replica holding the lock is already
            // doing this run. Release happens when the handle is disposed.
            var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();
            await using var handle = await distributedLock.TryAcquireAsync("bikontrol:job:reminders", cancellationToken);
            if (handle is null)
            {
                _logger.LogInformation("Reminder run skipped: another instance holds the lock.");
                _lastRunUtc = DateTime.UtcNow;
                return JobMetrics.Skipped;
            }

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

            JobMetrics.RecordReminders("generated", generated);
            JobMetrics.RecordReminders("emailed", emailed);
            JobMetrics.RecordReminders("pushed", pushed);

            _logger.LogInformation(
                "Reminder run complete: {Generated} generated, {Emailed} digest email(s), {Pushed} push delivery(ies).",
                generated,
                emailed,
                pushed);

            // Only a fully successful run counts: a failure retries on the next tick.
            _lastRunUtc = DateTime.UtcNow;
            return JobMetrics.Success;
        }
    }
}
