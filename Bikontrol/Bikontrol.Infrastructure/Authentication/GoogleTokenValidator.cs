using Bikontrol.Application.Interfaces;
using Google.Apis.Auth;

namespace Bikontrol.Infrastructure.Authentication
{
    /// <summary>Validates Google ID tokens with the Google.Apis.Auth library.</summary>
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        public async Task<GoogleIdentity?> ValidateAsync(string idToken, string clientId)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });

                return new GoogleIdentity(payload.Email, payload.EmailVerified == true, payload.Name);
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }
    }
}
