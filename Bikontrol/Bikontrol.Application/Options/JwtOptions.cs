using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>Access/refresh token settings (<c>Jwt</c> section). Validated at startup.</summary>
    public class JwtOptions
    {
        public const string Section = "Jwt";

        [Required, MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string KeyId { get; set; } = "k1";

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpireMinutes { get; set; } = 30;

        [Range(1, 365)]
        public int RefreshExpireDays { get; set; } = 30;

        /// <summary>How long a user's security stamp is cached by the bearer validation (0 disables the cache).</summary>
        [Range(0, 3600)]
        public int SecurityStampCacheSeconds { get; set; } = 30;

        /// <summary>Keys still accepted for validation while a signing-key rotation drains.</summary>
        public List<PreviousKey> PreviousKeys { get; set; } = [];

        public class PreviousKey
        {
            public string? Id { get; set; }
            public string? Key { get; set; }
        }
    }
}
