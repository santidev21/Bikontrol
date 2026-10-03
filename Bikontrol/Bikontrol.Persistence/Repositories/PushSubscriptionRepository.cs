using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class PushSubscriptionRepository : IPushSubscriptionRepository
    {
        private readonly AppDbContext _context;

        public PushSubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId)
        {
            return await _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<PushSubscription>> GetByUserIdsAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<PushSubscription>();

            return await _context.PushSubscriptions
                .Where(s => ids.Contains(s.UserId))
                .ToListAsync();
        }

        public async Task UpsertAsync(PushSubscription subscription)
        {
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);

            if (existing is null)
            {
                await _context.PushSubscriptions.AddAsync(subscription);
                return;
            }

            // Re-subscribe: keep the row, refresh keys and ownership.
            existing.UserId = subscription.UserId;
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
        }

        public async Task RemoveByEndpointAsync(string endpoint)
        {
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
            if (existing is not null)
                _context.PushSubscriptions.Remove(existing);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
