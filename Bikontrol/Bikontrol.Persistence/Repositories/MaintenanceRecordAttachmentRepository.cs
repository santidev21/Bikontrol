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
                .AsNoTracking()
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

        public async Task<long> GetTotalSizeBytesByUserAsync(Guid userId)
        {
            return await OwnedBy(userId).SumAsync(a => (long)a.SizeBytes);
        }

        public async Task<IReadOnlyList<string>> GetStorageKeysByUserAsync(Guid userId)
        {
            return await OwnedBy(userId)
                .Where(a => a.StorageKey != null)
                .Select(a => a.StorageKey!)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<MaintenanceRecordAttachment>> GetPendingStorageMigrationAsync(int take)
        {
            return await _context.MaintenanceRecordAttachments
                .Where(a => a.StorageKey == null && a.DataUrl != "")
                .OrderBy(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Guid?> GetOwnerIdAsync(Guid attachmentId)
        {
            return await (
                from a in _context.MaintenanceRecordAttachments
                join r in _context.MotorcycleMaintenanceRecords on a.MotorcycleMaintenanceRecordId equals r.Id
                join m in _context.Motorcycles on r.MotorcycleId equals m.Id
                where a.Id == attachmentId
                select (Guid?)m.UserId).FirstOrDefaultAsync();
        }

        // Attachments of every record of the user's motorcycles. Soft-deleted
        // motorcycles still count: their bytes stay until the account is purged.
        private IQueryable<MaintenanceRecordAttachment> OwnedBy(Guid userId) =>
            from a in _context.MaintenanceRecordAttachments
            join r in _context.MotorcycleMaintenanceRecords on a.MotorcycleMaintenanceRecordId equals r.Id
            join m in _context.Motorcycles on r.MotorcycleId equals m.Id
            where m.UserId == userId
            select a;

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
