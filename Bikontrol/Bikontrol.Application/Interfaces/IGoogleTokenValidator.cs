namespace Bikontrol.Application.Interfaces
{
    /// <summary>The verified identity carried by a Google ID token.</summary>
    public sealed record GoogleIdentity(string Email, bool EmailVerified, string? Name);

    /// <summary>Validates Google ID tokens (signature, audience, expiry).</summary>
    public interface IGoogleTokenValidator
    {
        /// <summary>
        /// Returns the identity in <paramref name="idToken"/> for the given OAuth
        /// client, or <c>null</c> when the token is not valid.
        /// </summary>
        Task<GoogleIdentity?> ValidateAsync(string idToken, string clientId);
    }
}
