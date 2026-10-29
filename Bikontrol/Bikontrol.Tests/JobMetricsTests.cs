using System.Diagnostics.Metrics;
using Bikontrol.Infrastructure.Observability;
using Xunit;

namespace Bikontrol.Tests;

public class JobMetricsTests
{
    [Fact]
    public void RecordRun_PublishesRunCounterAndLastSuccessGauge()
    {
        var runs = new List<(long Value, string? Job, string? Result)>();
        var lastSuccess = new List<(double Value, string? Job)>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == JobMetrics.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "bikontrol.job.runs")
            {
                runs.Add((value, Tag(tags, "task"), Tag(tags, "result")));
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "bikontrol.job.last_success.timestamp")
            {
                lastSuccess.Add((value, Tag(tags, "task")));
            }
        });
        listener.Start();

        JobMetrics.RecordRun("unit-test-job", JobMetrics.Success, TimeSpan.FromSeconds(1));
        JobMetrics.RecordRun("unit-test-job", JobMetrics.Failed, TimeSpan.FromSeconds(1));
        listener.RecordObservableInstruments();

        Assert.Contains(runs, r => r is { Value: 1, Job: "unit-test-job", Result: JobMetrics.Success });
        Assert.Contains(runs, r => r is { Value: 1, Job: "unit-test-job", Result: JobMetrics.Failed });
        var gauge = Assert.Single(lastSuccess, g => g.Job == "unit-test-job");
        Assert.True(gauge.Value > 1_700_000_000);
    }

    [Fact]
    public void RecordReminders_IgnoresZeroCounts()
    {
        var seen = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == JobMetrics.MeterName && instrument.Name == "bikontrol.reminders")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => seen.Add(value));
        listener.Start();

        JobMetrics.RecordReminders("generated", 0);
        JobMetrics.RecordReminders("generated", 3);

        Assert.Equal(new long[] { 3 }, seen);
    }

    [Fact]
    public async Task MeasureAsync_RecordsFailureAndRethrows()
    {
        var results = new List<string?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == JobMetrics.MeterName && instrument.Name == "bikontrol.job.runs")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (Tag(tags, "task") == "measure-failing-job")
            {
                results.Add(Tag(tags, "result"));
            }
        });
        listener.Start();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            JobMetrics.MeasureAsync("measure-failing-job", () => throw new InvalidOperationException("boom")));

        Assert.Equal(new[] { JobMetrics.Failed }, results);
    }

    private static string? Tag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
            {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
