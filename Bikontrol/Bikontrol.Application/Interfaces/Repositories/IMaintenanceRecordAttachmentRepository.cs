using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMaintenanceRecordAttachmentRepository
    {
        Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId);
        Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds);
        Task AddAsync(MaintenanceRecordAttachment entity);
        Task RemoveAsync(MaintenanceRecordAttachment entity);
        Task SaveChangesAsync();
    }
}
