using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Options;
using Bikontrol.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace Bikontrol.Application.Services
{
    public sealed class StorageQuota : IStorageQuota
    {
        private readonly IMaintenanceRecordAttachmentRepository _attachments;
        private readonly IMotorcycleRepository _motorcycles;
        private readonly StorageOptions _options;

        public StorageQuota(
            IMaintenanceRecordAttachmentRepository attachments,
            IMotorcycleRepository motorcycles,
            IOptions<StorageOptions> options)
        {
            _attachments = attachments;
            _motorcycles = motorcycles;
            _options = options.Value;
        }

        public async Task EnsureWithinAsync(Guid userId, long incomingBytes, long replacingBytes = 0)
        {
            if (_options.MaxBytesPerUser <= 0)
                return;

            var used = await _attachments.GetTotalSizeBytesByUserAsync(userId)
                + await _motorcycles.GetImageBytesByUserAsync(userId);

            if (used - replacingBytes + incomingBytes > _options.MaxBytesPerUser)
                throw new ValidationException("Alcanzaste el límite de almacenamiento de imágenes.", "storage_quota_exceeded");
        }
    }
}
