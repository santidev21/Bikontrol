namespace Bikontrol.Tests.Integration.Infrastructure;

/// <summary>
/// Hands out a unique synthetic client IP per call (in the benchmarking range
/// 198.18.0.0/15) so each test gets its own per-IP rate-limit partition. Using a
/// fixed IP (or a small random range) risks two tests sharing a partition and
/// one of them starting with part of its budget already spent.
/// </summary>
public static class TestClientIps
{
    private static int _sequence;

    public static string Next()
    {
        var n = Interlocked.Increment(ref _sequence);
        return $"198.18.{(n / 256) % 256}.{n % 256}";
    }
}
