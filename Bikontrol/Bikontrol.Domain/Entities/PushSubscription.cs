using Bikontrol.Persistence.Entities;
using System;

namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// A browser Web Push subscription for a user (one row per device/browser).
    /// Endpoint is unique: re-subscribing the same browser updates the keys
    /// instead of creating duplicates.
    /// </summary>
    public class PushSubscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Last time a push was delivered to this subscription.</summary>
        public DateTime? LastUsedAt { get; set; }

        public User? User { get; set; }
    }
}
