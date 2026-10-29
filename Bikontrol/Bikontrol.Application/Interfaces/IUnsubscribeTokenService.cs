namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Signed, stateless tokens for the email unsubscribe links. A token proves
    /// the link was minted by this server for a given user, so the unsubscribe
    /// endpoint needs no login (mail clients POST to it directly).
    /// </summary>
    public interface IUnsubscribeTokenService
    {
        string Create(Guid userId);

        bool TryValidate(string? token, out Guid userId);
    }
}
