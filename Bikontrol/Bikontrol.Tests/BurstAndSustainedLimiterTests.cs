using Bikontrol.API.RateLimiting;
using System;
using System.Threading.RateLimiting;

namespace Bikontrol.Tests;

public class BurstAndSustainedLimiterTests
{
    [Fact]
    public void AttemptAcquire_ShouldGrantThenRejectOnTheBurstWindow()
    {
        var limiter = new BurstAndSustainedLimiter(
            new FixedWindowRateLimiterOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(60) },
            new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) });

        Assert.True(limiter.AttemptAcquire().IsAcquired);
        Assert.True(limiter.AttemptAcquire().IsAcquired);
        // The sustained window still has budget, but the burst window is spent.
        Assert.False(limiter.AttemptAcquire().IsAcquired);
    }

    [Fact]
    public void AttemptAcquire_ShouldAlsoRejectOnTheSustainedWindow()
    {
        var limiter = new BurstAndSustainedLimiter(
            new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(60) },
            new FixedWindowRateLimiterOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(60) });

        Assert.True(limiter.AttemptAcquire().IsAcquired);
        Assert.True(limiter.AttemptAcquire().IsAcquired);
        // The burst window still has budget, but the long window is spent.
        Assert.False(limiter.AttemptAcquire().IsAcquired);
    }

    [Fact]
    public void AcquireAsync_ShouldRoundTripThroughTheAsyncPath()
    {
        var limiter = new BurstAndSustainedLimiter(
            new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromSeconds(60) },
            new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromSeconds(60) });

        var lease = limiter.AcquireAsync().AsTask().GetAwaiter().GetResult();

        Assert.True(lease.IsAcquired);
    }

    [Fact]
    public void IdleDuration_ShouldBeNullWhileEitherWindowIsHot()
    {
        var limiter = new BurstAndSustainedLimiter(
            new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(60) },
            new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(60) });

        limiter.AttemptAcquire();

        Assert.Null(limiter.IdleDuration);
    }
}
