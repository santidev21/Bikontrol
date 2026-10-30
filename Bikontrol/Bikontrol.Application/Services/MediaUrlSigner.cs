using System.Security.Cryptography;
using System.Text;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Microsoft.Extensions.Options;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// HMAC-SHA256 over <c>attachments/{id}.{exp}</c> with a purpose-specific key
    /// derived from <c>Jwt:Key</c>. Expiry is the end of the current 6-hour window
    /// plus one more window, so a URL lives 6–12 hours and is stable within a
    /// window (cacheable).
    /// </summary>
    public sealed class MediaUrlSigner : IMediaUrlSigner
    {
        public const int WindowSeconds = 6 * 60 * 60;
        private const string Purpose = "bikontrol:media:v1";

        private readonly byte[] _key;
        private readonly Func<DateTimeOffset> _clock;

        public MediaUrlSigner(IOptions<JwtOptions> jwt)
            : this(jwt, () => DateTimeOffset.UtcNow)
        {
        }

        public MediaUrlSigner(IOptions<JwtOptions> jwt, Func<DateTimeOffset> clock)
        {
            using var root = new HMACSHA256(Encoding.UTF8.GetBytes(jwt.Value.Key));
            _key = root.ComputeHash(Encoding.UTF8.GetBytes(Purpose));
            _clock = clock;
        }

        public string CreateAttachmentUrl(Guid attachmentId)
        {
            var now = _clock().ToUnixTimeSeconds();
            var expires = ((now / WindowSeconds) + 2) * WindowSeconds;
            return $"/api/media/attachments/{attachmentId}?exp={expires}&sig={Sign(attachmentId, expires)}";
        }

        public bool IsValidAttachmentUrl(Guid attachmentId, long expiresUnix, string? signature)
        {
            if (string.IsNullOrEmpty(signature) || expiresUnix < _clock().ToUnixTimeSeconds())
                return false;

            var expected = Encoding.ASCII.GetBytes(Sign(attachmentId, expiresUnix));
            var provided = Encoding.ASCII.GetBytes(signature);
            return CryptographicOperations.FixedTimeEquals(expected, provided);
        }

        private string Sign(Guid attachmentId, long expiresUnix)
        {
            var data = Encoding.UTF8.GetBytes($"attachments/{attachmentId}.{expiresUnix}");
            return Convert.ToBase64String(HMACSHA256.HashData(_key, data))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
