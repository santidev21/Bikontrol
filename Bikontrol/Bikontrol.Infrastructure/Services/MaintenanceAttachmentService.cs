using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
{
    /// <summary>
    /// Photos/documents attached to a maintenance record. Ownership is verified
    /// through the record's motorcycle, and the demo tenant is read-only.
    /// </summary>
    public class MaintenanceAttachmentService : IMaintenanceAttachmentService
    {
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IMaintenanceRecordAttachmentRepository _attachmentRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly ICurrentUserService _current;
        private readonly IMapper _mapper;

        public MaintenanceAttachmentService(
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMaintenanceRecordAttachmentRepository attachmentRepository,
            IMotorcycleRepository motorcycleRepository,
            ICurrentUserService current,
            IMapper mapper)
        {
            _recordRepository = recordRepository;
            _attachmentRepository = attachmentRepository;
            _motorcycleRepository = motorcycleRepository;
            _current = current;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<MaintenanceAttachmentDTO>> GetByRecordAsync(Guid recordId)
        {
            await EnsureOwnedRecordAsync(recordId);
            var attachments = await _attachmentRepository.GetByRecordIdAsync(recordId);
            return _mapper.Map<IReadOnlyList<MaintenanceAttachmentDTO>>(attachments);
        }

        public async Task<MaintenanceAttachmentDTO> AddAsync(Guid recordId, AddAttachmentRequest request)
        {
            EnsureCanWrite();
            await EnsureOwnedRecordAsync(recordId);
            ImageDataUrlValidator.Validate(request.DataUrl, required: true);

            var attachment = new MaintenanceRecordAttachment
            {
                MotorcycleMaintenanceRecordId = recordId,
                DataUrl = request.DataUrl,
                FileName = request.FileName?.Trim(),
                ContentType = ImageDataUrlValidator.ContentTypeOf(request.DataUrl)
            };
            await _attachmentRepository.AddAsync(attachment);
            await _attachmentRepository.SaveChangesAsync();
            return _mapper.Map<MaintenanceAttachmentDTO>(attachment);
        }

        public async Task DeleteAsync(Guid recordId, Guid attachmentId)
        {
            EnsureCanWrite();
            await EnsureOwnedRecordAsync(recordId);

            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment is null || attachment.MotorcycleMaintenanceRecordId != recordId)
                throw new NotFoundException("Adjunto no encontrado.");

            await _attachmentRepository.RemoveAsync(attachment);
            await _attachmentRepository.SaveChangesAsync();
        }

        private async Task EnsureOwnedRecordAsync(Guid recordId)
        {
            var record = await _recordRepository.GetByIdAsync(recordId);
            if (record is null)
                throw new NotFoundException("Registro de mantenimiento no encontrado.");

            var motorcycle = await _motorcycleRepository.GetByIdAsync(record.MotorcycleId);
            if (motorcycle is null || motorcycle.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para este registro.");
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }
    }
}
