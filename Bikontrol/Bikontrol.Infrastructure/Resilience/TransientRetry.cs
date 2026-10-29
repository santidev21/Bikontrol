namespace Bikontrol.Infrastructure.Resilience
{
    /// <summary>
    /// Minimal retry with exponential backoff for calls to flaky third parties
    /// (SMTP, push services). Only exceptions the caller classifies as
    /// transient are retried; anything else surfaces immediately.
    /// </summary>
    public static class TransientRetry
    {
        public static async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            Func<Exception, bool> isTransient,
            int maxAttempts = 3,
            TimeSpan? baseDelay = null,
            Func<TimeSpan, CancellationToken, Task>? delay = null,
            CancellationToken cancellationToken = default)
        {
            var wait = delay ?? Task.Delay;
            var first = baseDelay ?? TimeSpan.FromMilliseconds(500);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (attempt < maxAttempts && isTransient(ex) && !cancellationToken.IsCancellationRequested)
                {
                    await wait(TimeSpan.FromTicks(first.Ticks * (1L << (attempt - 1))), cancellationToken);
                }
            }
        }
    }
}
