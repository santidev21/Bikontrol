using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IPushSubscriptionRepository
    {
        /// <summary>Subscriptions of a user (for sending pushes).</summary>
        Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId);

        /// <summary>All subscriptions of users with a given set of ids.</summary>
        Task<IReadOnlyList<PushSubscription>> GetByUserIdsAsync(IEnumerable<Guid> userIds);

        /// <summary>
        /// Creates the subscription or updates its keys when the endpoint
        /// already exists (browser re-subscribe).
        /// </summary>
        Task UpsertAsync(PushSubscription subscription);

        /// <summary>
        /// Removes a subscription by endpoint. Pass <paramref name="ownerUserId"/>
        /// for a user-initiated unsubscribe so nobody can drop another user's
        /// endpoint; leave it null only for system cleanup of dead endpoints.
        /// </summary>
        Task RemoveByEndpointAsync(string endpoint, Guid? ownerUserId = null);

        Task SaveChangesAsync();
    }
}
