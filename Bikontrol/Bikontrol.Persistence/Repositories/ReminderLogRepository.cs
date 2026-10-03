using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class ReminderLogRepository : IReminderLogRepository
    {
        private readonly AppDbContext _context;

        public ReminderLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsSinceAsync(Guid userMaintenanceId, string kind, DateTime since)
        {
            return await _context.ReminderLogs
                .AnyAsync(r => r.UserMaintenanceId == userMaintenanceId
                    && r.Kind == kind
                    && r.CreatedAt >= since);
        }

        public async Task<IReadOnlySet<Guid>> GetRecentUserMaintenanceIdsAsync(
            IEnumerable<Guid> userMaintenanceIds,
            string kind,
            DateTime since)
        {
            var ids = userMaintenanceIds.Distinct().ToList();
            if (ids.Count == 0)
                return new HashSet<Guid>();

            var recent = await _context.ReminderLogs
                .Where(r => ids.Contains(r.UserMaintenanceId) && r.Kind == kind && r.CreatedAt >= since)
                .Select(r => r.UserMaintenanceId)
                .Distinct()
                .ToListAsync();

            return recent.ToHashSet();
        }

        public async Task AddAsync(ReminderLog entity)
        {
            await _context.ReminderLogs.AddAsync(entity);
        }

        public async Task<IReadOnlyList<ReminderLog>> GetPendingWithDetailsAsync(string channel)
        {
            return await _context.ReminderLogs
                .Include(r => r.User)
                .Include(r => r.UserMaintenance)
                .Where(r => r.Channel == channel)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkDeliveredAsync(IEnumerable<Guid> ids, string channel, DateTime deliveredAt)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
                return;

            var logs = await _context.ReminderLogs
                .Where(r => idList.Contains(r.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.Channel = channel;
                log.DeliveredAt = deliveredAt;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
