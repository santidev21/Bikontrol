using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public User? User { get; set; }

        private RefreshToken() { }

        public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            TokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
        }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
        }
    }
}
