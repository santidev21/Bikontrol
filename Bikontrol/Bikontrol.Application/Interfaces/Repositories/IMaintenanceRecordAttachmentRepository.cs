using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMaintenanceRecordAttachmentRepository
    {
        Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId);
        Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds);
        /// <summary>Sum of stored image bytes across every record of the user's motorcycles.</summary>
        Task<long> GetTotalSizeBytesByUserAsync(Guid userId);

        /// <summary>Object keys of the user's attachments that live in object storage (account deletion).</summary>
        Task<IReadOnlyList<string>> GetStorageKeysByUserAsync(Guid userId);

        /// <summary>Attachments whose image is still in the database, oldest first (storage migration).</summary>
        Task<IReadOnlyList<MaintenanceRecordAttachment>> GetPendingStorageMigrationAsync(int take);

        /// <summary>Owner of the attachment (through record and motorcycle), for object keys and quotas.</summary>
        Task<Guid?> GetOwnerIdAsync(Guid attachmentId);

        Task AddAsync(MaintenanceRecordAttachment entity);
        Task RemoveAsync(MaintenanceRecordAttachment entity);
        Task SaveChangesAsync();
    }
}
