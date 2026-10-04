using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class MaintenanceRecordAttachmentRepository : IMaintenanceRecordAttachmentRepository
    {
        private readonly AppDbContext _context;

        public MaintenanceRecordAttachmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id)
        {
            return await _context.MaintenanceRecordAttachments.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId)
        {
            return await _context.MaintenanceRecordAttachments
                .Where(a => a.MotorcycleMaintenanceRecordId == recordId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds)
        {
            var ids = recordIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<MaintenanceRecordAttachment>();

            return await _context.MaintenanceRecordAttachments
                .Where(a => ids.Contains(a.MotorcycleMaintenanceRecordId))
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(MaintenanceRecordAttachment entity)
        {
            await _context.MaintenanceRecordAttachments.AddAsync(entity);
        }

        public Task RemoveAsync(MaintenanceRecordAttachment entity)
        {
            _context.MaintenanceRecordAttachments.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
