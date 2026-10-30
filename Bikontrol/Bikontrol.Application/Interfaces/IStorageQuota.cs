namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// The per-user cap on stored image bytes (<c>Storage:MaxBytesPerUser</c>),
    /// shared by attachments and motorcycle photos so they draw from one budget.
    /// </summary>
    public interface IStorageQuota
    {
        /// <summary>
        /// Throws <c>storage_quota_exceeded</c> when adding <paramref name="incomingBytes"/>
        /// (minus <paramref name="replacingBytes"/> that the new image frees) would pass the cap.
        /// </summary>
        Task EnsureWithinAsync(Guid userId, long incomingBytes, long replacingBytes = 0);
    }
}
