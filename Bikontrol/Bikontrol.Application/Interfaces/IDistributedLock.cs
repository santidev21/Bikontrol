namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Cross-instance mutual exclusion for background jobs, so a scaled-out API
    /// runs each job once at a time.
    /// </summary>
    public interface IDistributedLock
    {
        /// <summary>
        /// Tries to take the named lock without waiting. Returns a handle that
        /// releases it on dispose, or <c>null</c> when another holder has it.
        /// </summary>
        Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken = default);
    }
}
