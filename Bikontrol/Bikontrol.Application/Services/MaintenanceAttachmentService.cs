using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Bikontrol.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Photos/documents attached to a maintenance record. Ownership is verified
    /// through the record's motorcycle, and the demo tenant is read-only. Images
    /// go to object storage when it is enabled (otherwise they stay in the
    /// database); either way the API hands out signed same-origin URLs, and a
    /// per-user quota caps the stored bytes.
    /// </summary>
    public class MaintenanceAttachmentService : IMaintenanceAttachmentService
    {
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IMaintenanceRecordAttachmentRepository _attachmentRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly ICurrentUserService _current;
        private readonly IObjectStorage _storage;
        private readonly IMediaUrlSigner _urlSigner;
        private readonly IStorageQuota _quota;
        private readonly ILogger<MaintenanceAttachmentService> _logger;

        public MaintenanceAttachmentService(
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMaintenanceRecordAttachmentRepository attachmentRepository,
            IMotorcycleRepository motorcycleRepository,
            ICurrentUserService current,
            IObjectStorage storage,
            IMediaUrlSigner urlSigner,
            IStorageQuota quota,
            ILogger<MaintenanceAttachmentService> logger)
        {
            _recordRepository = recordRepository;
            _attachmentRepository = attachmentRepository;
            _motorcycleRepository = motorcycleRepository;
            _current = current;
            _storage = storage;
            _urlSigner = urlSigner;
            _quota = quota;
            _logger = logger;
        }

        public async Task<IReadOnlyList<MaintenanceAttachmentDTO>> GetByRecordAsync(Guid recordId)
        {
            await EnsureOwnedRecordAsync(recordId);
            var attachments = await _attachmentRepository.GetByRecordIdAsync(recordId);
            return attachments.Select(ToDto).ToList();
        }

        public async Task<MaintenanceAttachmentDTO> AddAsync(Guid recordId, AddAttachmentRequest request)
        {
            EnsureCanWrite();
            await EnsureOwnedRecordAsync(recordId);
            ImageDataUrlValidator.Validate(request.DataUrl, required: true);

            var bytes = ImageDataUrlValidator.Decode(request.DataUrl);
            await _quota.EnsureWithinAsync(_current.UserId, bytes.Length);

            var contentType = ImageDataUrlValidator.ContentTypeOf(request.DataUrl);
            var attachment = new MaintenanceRecordAttachment
            {
                MotorcycleMaintenanceRecordId = recordId,
                FileName = request.FileName?.Trim(),
                ContentType = contentType,
                SizeBytes = bytes.Length
            };

            if (_storage.IsEnabled)
            {
                // Upload first: a failed upload leaves no row behind, while a failed
                // SaveChanges leaves one orphan object (cleaned up below).
                attachment.StorageKey = StorageKeys.ForAttachment(_current.UserId, attachment.Id, contentType);
                await _storage.PutAsync(attachment.StorageKey, bytes, contentType);
            }
            else
            {
                attachment.DataUrl = request.DataUrl;
            }

            try
            {
                await _attachmentRepository.AddAsync(attachment);
                await _attachmentRepository.SaveChangesAsync();
            }
            catch when (attachment.StorageKey is not null)
            {
                await TryDeleteObjectsAsync(new[] { attachment.StorageKey });
                throw;
            }

            return ToDto(attachment);
        }

        public async Task DeleteAsync(Guid recordId, Guid attachmentId)
        {
            EnsureCanWrite();
            await EnsureOwnedRecordAsync(recordId);

            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment is null || attachment.MotorcycleMaintenanceRecordId != recordId)
                throw new NotFoundException("Adjunto no encontrado.", "attachment_not_found");

            await _attachmentRepository.RemoveAsync(attachment);
            await _attachmentRepository.SaveChangesAsync();

            if (attachment.StorageKey is not null)
                await TryDeleteObjectsAsync(new[] { attachment.StorageKey });
        }

        public async Task<StoredObject> OpenMediaAsync(Guid attachmentId, long expiresUnix, string? signature, CancellationToken cancellationToken = default)
        {
            if (!_urlSigner.IsValidAttachmentUrl(attachmentId, expiresUnix, signature))
                throw new NotFoundException("Archivo no encontrado.", "attachment_not_found");

            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId)
                ?? throw new NotFoundException("Archivo no encontrado.", "attachment_not_found");

            if (attachment.StorageKey is not null)
            {
                return await _storage.GetAsync(attachment.StorageKey, cancellationToken)
                    ?? throw new NotFoundException("Archivo no encontrado.", "attachment_not_found");
            }

            // Not migrated yet: the image still lives in the database.
            var bytes = ImageDataUrlValidator.Decode(attachment.DataUrl);
            return new StoredObject(new MemoryStream(bytes, writable: false), attachment.ContentType, bytes.Length);
        }

        /// <summary>
        /// <see cref="MaintenanceAttachmentDTO.DataUrl"/> is always a signed media
        /// URL, even for rows still stored in the database, so list payloads never
        /// carry megabytes of base64.
        /// </summary>
        private MaintenanceAttachmentDTO ToDto(MaintenanceRecordAttachment attachment)
        {
            var dto = attachment.ToAttachmentDTO();
            dto.DataUrl = _urlSigner.CreateAttachmentUrl(attachment.Id);
            return dto;
        }

        // Best effort: a leftover object costs a few KB, a failed request costs the user.
        private async Task TryDeleteObjectsAsync(IEnumerable<string> keys)
        {
            try
            {
                await _storage.DeleteAsync(keys);
            }
            catch (Exception ex)
            {
                LogOrphanedObjects(_logger, keys.Count(), ex);
            }
        }

        private async Task EnsureOwnedRecordAsync(Guid recordId)
        {
            var record = await _recordRepository.GetByIdAsync(recordId);
            if (record is null)
                throw new NotFoundException("Registro de mantenimiento no encontrado.", "record_not_found");

            var motorcycle = await _motorcycleRepository.GetByIdAsync(record.MotorcycleId);
            if (motorcycle is null || motorcycle.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para este registro.", "record_no_access");
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.", "demo_read_only");
        }

        private static readonly Action<ILogger, int, Exception?> LogOrphanedObjects = LoggerMessage.Define<int>(
            LogLevel.Warning, new EventId(1, nameof(LogOrphanedObjects)), "Could not delete {Count} object(s) from storage; they are orphaned.");
    }
}
