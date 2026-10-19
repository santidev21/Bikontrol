using System;
using System.Threading;
using System.Threading.RateLimiting;

namespace Bikontrol.API.RateLimiting
{
    /// <summary>
    /// A limiter enforcing two independent fixed windows over one key: a short
    /// burst window and a long sustained ceiling. Needed because the
    /// auth endpoints must enforce both, while an action-level
    /// EnableRateLimiting attribute replaces the class-level policy instead of
    /// adding to it (so two chained attributes are not an option).
    /// Acquire order: burst first, so rejected requests never consume the
    /// shared hourly budget. When the sustained window rejects, the burst
    /// permit is already spent — acceptable, the request did arrive.
    /// </summary>
    public sealed class BurstAndSustainedLimiter : RateLimiter
    {
        private readonly FixedWindowRateLimiter _burst;
        private readonly FixedWindowRateLimiter _sustained;

        public BurstAndSustainedLimiter(
            FixedWindowRateLimiterOptions burstOptions,
            FixedWindowRateLimiterOptions sustainedOptions)
        {
            _burst = new FixedWindowRateLimiter(burstOptions);
            _sustained = new FixedWindowRateLimiter(sustainedOptions);
        }

        public override TimeSpan? IdleDuration
        {
            get
            {
                var burstIdle = _burst.IdleDuration;
                var sustainedIdle = _sustained.IdleDuration;
                if (burstIdle is null || sustainedIdle is null)
                    return null;
                return burstIdle.Value > sustainedIdle.Value ? burstIdle : sustainedIdle;
            }
        }

        public override RateLimiterStatistics? GetStatistics() => _burst.GetStatistics();

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            var burstLease = _burst.AttemptAcquire(permitCount);
            if (!burstLease.IsAcquired)
                return burstLease;

            var sustainedLease = _sustained.AttemptAcquire(permitCount);
            if (!sustainedLease.IsAcquired)
                return sustainedLease;

            // Both granted: fixed-window leases are no-ops on dispose, so
            // exposing either one as the result is safe.
            return sustainedLease;
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
            => ValueTask.FromResult(AttemptAcquireCore(permitCount));
    }
}
