using Bikontrol.Application.DTOs.Maintenance;

namespace Bikontrol.Application.Interfaces
{
    public interface IMaintenanceAttachmentService
    {
        /// <summary>Attachments of a maintenance record (ownership enforced).</summary>
        Task<IReadOnlyList<MaintenanceAttachmentDTO>> GetByRecordAsync(Guid recordId);

        /// <summary>Adds a photo/document to a maintenance record.</summary>
        Task<MaintenanceAttachmentDTO> AddAsync(Guid recordId, AddAttachmentRequest request);

        /// <summary>Deletes an attachment the user owns.</summary>
        Task DeleteAsync(Guid recordId, Guid attachmentId);
    }
}
