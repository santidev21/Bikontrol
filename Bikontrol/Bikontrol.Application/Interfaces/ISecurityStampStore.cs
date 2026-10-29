namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Read side of the per-user security stamp, used to reject access tokens
    /// minted before a password change/reset or account deletion.
    /// </summary>
    public interface ISecurityStampStore
    {
        /// <summary>Current stamp, or <c>null</c> when the user no longer exists.</summary>
        Task<Guid?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
