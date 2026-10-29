using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class MotorcycleMaintenanceRecordRepository : IMotorcycleMaintenanceRecordRepository
    {
        private readonly AppDbContext _context;

        public MotorcycleMaintenanceRecordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity)
        {
            await _context.MotorcycleMaintenanceRecords.AddAsync(entity);
            return entity;
        }

        public async Task<MotorcycleMaintenanceRecord?> GetByIdAsync(Guid id)
        {
            return await _context.MotorcycleMaintenanceRecords
                .Include(x => x.UserMaintenance)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId, int? limit = null)
        {
            var query = _context.MotorcycleMaintenanceRecords
                .Include(x => x.UserMaintenance)
                .AsNoTracking()
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.PerformedAt)
                .ThenByDescending(x => x.CreatedAt);

            if (limit.HasValue)
                query = (IOrderedQueryable<MotorcycleMaintenanceRecord>)query.Take(limit.Value);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<MotorcycleMaintenanceRecord>();

            return await _context.MotorcycleMaintenanceRecords
                .Include(x => x.UserMaintenance)
                .AsNoTracking()
                .Where(x => ids.Contains(x.MotorcycleId))
                .OrderByDescending(x => x.PerformedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId)
        {
            return await _context.MotorcycleMaintenanceRecords
                .AsNoTracking()
                .Where(x => x.UserMaintenanceId == userMaintenanceId)
                .OrderByDescending(x => x.PerformedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds)
        {
            var ids = userMaintenanceIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, MotorcycleMaintenanceRecord>();

            // Latest record per maintenance item without pulling every record:
            // keep a row only when no newer one exists for the same item.
            var latest = await _context.MotorcycleMaintenanceRecords
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserMaintenanceId))
                .Where(x => !_context.MotorcycleMaintenanceRecords.Any(y =>
                    y.UserMaintenanceId == x.UserMaintenanceId
                    && (y.PerformedAt > x.PerformedAt
                        || (y.PerformedAt == x.PerformedAt && y.CreatedAt > x.CreatedAt))))
                .ToListAsync();

            // Exact ties on (PerformedAt, CreatedAt) are practically impossible;
            // pick deterministically if one ever happens.
            return latest
                .GroupBy(x => x.UserMaintenanceId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
