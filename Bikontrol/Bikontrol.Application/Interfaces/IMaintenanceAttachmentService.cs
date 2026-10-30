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

        /// <summary>
        /// Opens the image behind a signed media URL. Anonymous by design (an
        /// <c>&lt;img&gt;</c> cannot send a bearer token): the signature is the
        /// credential. An invalid or expired signature and an unknown attachment
        /// are indistinguishable (not found).
        /// </summary>
        Task<StoredObject> OpenMediaAsync(Guid attachmentId, long expiresUnix, string? signature, CancellationToken cancellationToken = default);
    }
}
