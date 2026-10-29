using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bikontrol.Infrastructure.Observability
{
    /// <summary>
    /// Business and background-job metrics, exported through OpenTelemetry under
    /// the <see cref="MeterName"/> meter. Labels are bounded (job name, result,
    /// stage) — never user or motorcycle ids.
    /// </summary>
    public static class JobMetrics
    {
        public const string MeterName = "Bikontrol.Jobs";

        public const string Success = "success";
        public const string Skipped = "skipped";
        public const string Failed = "failed";

        private static readonly Meter Meter = new(MeterName);

        private static readonly Counter<long> Runs =
            Meter.CreateCounter<long>("bikontrol.job.runs", description: "Background job runs by result.");

        private static readonly Histogram<double> Duration =
            Meter.CreateHistogram<double>("bikontrol.job.duration", unit: "s", description: "Background job run duration.");

        private static readonly Counter<long> Reminders =
            Meter.CreateCounter<long>("bikontrol.reminders", description: "Reminders generated and delivered, by stage.");

        private static readonly Counter<long> RetentionDeleted =
            Meter.CreateCounter<long>("bikontrol.retention.deleted", description: "Rows purged by the retention job, by kind.");

        private static readonly ConcurrentDictionary<string, double> LastSuccessUnix = new();

        // Exposed as bikontrol_job_last_success_timestamp_seconds{task="..."}; the
        // "job did not succeed for over a day" alert is built on it.
        private static readonly ObservableGauge<double> LastSuccess = Meter.CreateObservableGauge(
            "bikontrol.job.last_success.timestamp",
            () => LastSuccessUnix.Select(p => new Measurement<double>(p.Value, new KeyValuePair<string, object?>("task", p.Key))),
            unit: "s",
            description: "Unix time of the last successful run of each job.");

        /// <summary>
        /// Runs a job body that reports <see cref="Success"/> or <see cref="Skipped"/>
        /// and records the outcome and duration; an exception counts as
        /// <see cref="Failed"/> and still propagates to the caller's retry logic.
        /// </summary>
        public static async Task MeasureAsync(string job, Func<Task<string>> run)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = Failed;
            try
            {
                result = await run();
            }
            finally
            {
                RecordRun(job, result, stopwatch.Elapsed);
            }
        }

        public static void RecordRun(string job, string result, TimeSpan elapsed)
        {
            Runs.Add(1, new KeyValuePair<string, object?>("task", job), new KeyValuePair<string, object?>("result", result));
            Duration.Record(elapsed.TotalSeconds, new KeyValuePair<string, object?>("task", job));
            if (result == Success)
            {
                LastSuccessUnix[job] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        public static void RecordReminders(string stage, int count)
        {
            if (count > 0)
            {
                Reminders.Add(count, new KeyValuePair<string, object?>("stage", stage));
            }
        }

        public static void RecordRetention(string kind, int count)
        {
            if (count > 0)
            {
                RetentionDeleted.Add(count, new KeyValuePair<string, object?>("kind", kind));
            }
        }
    }
}
