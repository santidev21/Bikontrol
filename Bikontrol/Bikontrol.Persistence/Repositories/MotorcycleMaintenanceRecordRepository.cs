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
            _context.MotorcycleMaintenanceRecords.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleMaintenanceRecords
                .Include(x => x.UserMaintenance)
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.PerformedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId)
        {
            return await _context.MotorcycleMaintenanceRecords
                .Where(x => x.UserMaintenanceId == userMaintenanceId)
                .OrderByDescending(x => x.PerformedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
