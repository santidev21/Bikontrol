using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        /// <summary>
        /// Groups every token minted by rotating one login. Presenting an
        /// already-rotated token means the chain leaked, so the whole family
        /// is revoked.
        /// </summary>
        public Guid FamilyId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public User? User { get; set; }

        private RefreshToken() { }

        public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt, Guid? familyId = null)
        {
            Id = Guid.NewGuid();
            FamilyId = familyId ?? Guid.NewGuid();
            UserId = userId;
            TokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
        }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

        /// <summary>True when the token was rotated or revoked at least <paramref name="grace"/> ago.</summary>
        public bool IsReused(TimeSpan grace) =>
            RevokedAt is { } revoked && DateTime.UtcNow - revoked > grace;

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
        }
    }
}
