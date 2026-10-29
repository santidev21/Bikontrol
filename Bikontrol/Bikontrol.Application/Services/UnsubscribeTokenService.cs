using System.Security.Cryptography;
using System.Text;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Microsoft.Extensions.Options;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// <c>base64url(userId).base64url(HMAC-SHA256(userId))</c>, keyed with a
    /// purpose-specific key derived from <c>Jwt:Key</c> so a token can never be
    /// replayed as anything else. Tokens do not expire: an unsubscribe link in an
    /// old email must keep working.
    /// </summary>
    public sealed class UnsubscribeTokenService : IUnsubscribeTokenService
    {
        private const string Purpose = "bikontrol:email-unsubscribe:v1";

        private readonly byte[] _key;

        public UnsubscribeTokenService(IOptions<JwtOptions> jwt)
        {
            using var root = new HMACSHA256(Encoding.UTF8.GetBytes(jwt.Value.Key));
            _key = root.ComputeHash(Encoding.UTF8.GetBytes(Purpose));
        }

        public string Create(Guid userId)
        {
            var id = userId.ToByteArray();
            return $"{Base64Url(id)}.{Base64Url(Sign(id))}";
        }

        public bool TryValidate(string? token, out Guid userId)
        {
            userId = Guid.Empty;
            var parts = token?.Split('.');
            if (parts is not { Length: 2 })
            {
                return false;
            }

            byte[] id;
            byte[] signature;
            try
            {
                id = FromBase64Url(parts[0]);
                signature = FromBase64Url(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (id.Length != 16 || !CryptographicOperations.FixedTimeEquals(signature, Sign(id)))
            {
                return false;
            }

            userId = new Guid(id);
            return true;
        }

        private byte[] Sign(byte[] data) => HMACSHA256.HashData(_key, data);

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
            return Convert.FromBase64String(padded);
        }
    }
}
