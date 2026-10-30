using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Bikontrol.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Services
{
    public class MotorcycleService : IMotorcycleService
    {
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IKmHistoryRepository _kmHistoryRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly ICurrentUserService _currentUser;
        private readonly ITransactionManager _transactions;
        private readonly IObjectStorage _storage;
        private readonly IMediaUrlSigner _urlSigner;
        private readonly IStorageQuota _quota;
        private readonly ILogger<MotorcycleService> _logger;

        private static readonly Action<ILogger, int, Exception?> LogOrphanedObjects = LoggerMessage.Define<int>(
            LogLevel.Warning, new EventId(1, nameof(LogOrphanedObjects)), "Could not delete {Count} photo object(s) from storage; they are orphaned.");

        public MotorcycleService(IMotorcycleRepository motorcycleRepository, IKmHistoryRepository kmHistoryRepository,
            IKmHistoryService kmHistoryService, ICurrentUserService currentUser,
            ITransactionManager transactions, IObjectStorage storage, IMediaUrlSigner urlSigner,
            IStorageQuota quota, ILogger<MotorcycleService> logger)
        {
            _storage = storage;
            _urlSigner = urlSigner;
            _quota = quota;
            _logger = logger;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryRepository = kmHistoryRepository;
            _kmHistoryService = kmHistoryService;
            _currentUser = currentUser;
            _transactions = transactions;
        }

        private void EnsureCanWrite()
        {
            if (_currentUser.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.", "demo_read_only");
        }

        public async Task<MotorcycleDTO> CreateAsync(SaveMotorcycleDTO dto)
        {
            EnsureCanWrite();
            var plan = PlanImage(dto.Image, existingId: null);
            var entity = new Motorcycle(dto.Name, dto.Brand, dto.Year, dto.Nickname, dto.Displacement, dto.Plate, _currentUser.UserId)
            {
                // The photo key embeds the motorcycle id, so it must exist before the insert.
                Id = Guid.NewGuid()
            };
            entity.Validate();

            var change = await ApplyImageAsync(entity, plan);

            try
            {
                // Moto + km inicial en una sola transacción: antes eran dos
                // SaveChanges separados y un fallo dejaba la moto sin historial.
                return await _transactions.ExecuteInTransactionAsync(async () =>
                {
                    var created = await _motorcycleRepository.AddAsync(entity);
                    var kmHistory = new MotorcycleKmHistory
                    {
                        MotorcycleId = created.Id,
                        Km = dto.Km
                    };
                    await _kmHistoryRepository.AddAsync(kmHistory);
                    await _kmHistoryRepository.SaveChangesAsync();
                    var createdDto = ToDto(created);
                    createdDto.Km = kmHistory.Km;
                    return createdDto;
                });
            }
            catch when (change.NewKey is not null)
            {
                await TryDeleteObjectsAsync(new[] { change.NewKey });
                throw;
            }
        }

        public async Task<MotorcycleDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);

            if (entity is null)
                throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes acceso a esta motocicleta.", "motorcycle_no_access");

            var dto = ToDto(entity);
            dto.Km = await GetCurrentKmOrDefaultAsync(entity.Id);
            return dto;
        }

        public async Task<IList<MotorcycleDTO>> GetByCurrentUserAsync()
        {
            var motorcycles = (await _motorcycleRepository.GetByUserIdAsync(_currentUser.UserId)).ToList();
            var dtos = motorcycles.Select(ToDto).ToList();

            var kmByMotorcycle = await _kmHistoryRepository.GetLatestKmByMotorcycleIdsAsync(
                motorcycles.Select(m => m.Id));

            foreach (var dto in dtos)
            {
                dto.Km = kmByMotorcycle.TryGetValue(dto.Id, out var km) ? km : 0;
            }

            return dtos;
        }

        private async Task<int> GetCurrentKmOrDefaultAsync(Guid motorcycleId)
        {
            var last = await _kmHistoryRepository.GetLastByMotorcycleIdAsync(motorcycleId);
            return last?.Km ?? 0;
        }

        public async Task<int> GetCurrentKmAsync(Guid id)
        {
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para ver esta motocicleta.", "motorcycle_no_access");

            return await _kmHistoryService.GetCurrentKmAsync(id);
        }

        public async Task AddKmHistoryAsync(Guid id, int km)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.", "motorcycle_no_access");

            await _kmHistoryService.AddKmAsync(id, km);
        }

        public async Task RollbackLastKmAsync(Guid id, int newKm)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");
            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.", "motorcycle_no_access");

            await _kmHistoryService.RollbackLastKmAsync(id, newKm);
        }

        public async Task UpdateAsync(Guid id, SaveMotorcycleDTO dto)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar esta motocicleta.", "motorcycle_no_access");

            // After the ownership check, so a foreign id is reported as such rather than as a bad image.
            var plan = PlanImage(dto.Image, existingId: id);

            entity.Name = dto.Name;
            entity.Brand = dto.Brand;
            entity.Year = dto.Year;
            entity.Nickname = dto.Nickname;
            entity.Displacement = dto.Displacement;
            entity.Plate = dto.Plate;
            entity.Validate();

            var change = await ApplyImageAsync(entity, plan);

            try
            {
                await _motorcycleRepository.UpdateAsync(entity);
                await _motorcycleRepository.SaveChangesAsync();
            }
            catch when (change.NewKey is not null)
            {
                await TryDeleteObjectsAsync(new[] { change.NewKey });
                throw;
            }

            // Only after the new photo is safely saved.
            if (change.OldKey is not null)
                await TryDeleteObjectsAsync(new[] { change.OldKey });
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            EnsureCanWrite();
            var entity = await _motorcycleRepository.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");

            if (entity.UserId != _currentUser.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar esta motocicleta.", "motorcycle_no_access");

            // The maintenance history stays, the photo does not: dropping it frees the
            // user's storage quota (a soft-deleted bike would otherwise count forever).
            var oldKey = ClearImage(entity);

            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _motorcycleRepository.SoftDeleteAsync(id);
                await _motorcycleRepository.SaveChangesAsync();
            });

            if (oldKey is not null)
                await TryDeleteObjectsAsync(new[] { oldKey });
        }

        public async Task<StoredObject> OpenImageAsync(Guid id, long expiresUnix, string? signature, CancellationToken cancellationToken = default)
        {
            if (!_urlSigner.IsValidMotorcycleUrl(id, expiresUnix, signature))
                throw new NotFoundException("Archivo no encontrado.", "image_not_found");

            var entity = await _motorcycleRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Archivo no encontrado.", "image_not_found");

            if (entity.ImageKey is not null)
            {
                return await _storage.GetAsync(entity.ImageKey, cancellationToken)
                    ?? throw new NotFoundException("Archivo no encontrado.", "image_not_found");
            }

            // Not migrated yet: the photo still lives in the database.
            if (!entity.Image.StartsWith("data:", StringComparison.Ordinal))
                throw new NotFoundException("Archivo no encontrado.", "image_not_found");

            var bytes = ImageDataUrlValidator.Decode(entity.Image);
            return new StoredObject(new MemoryStream(bytes, writable: false), ImageDataUrlValidator.ContentTypeOf(entity.Image), bytes.Length);
        }

        // ---- photo handling -------------------------------------------------------

        private enum ImageChangeKind { Keep, Remove, Replace }

        private sealed record ImagePlan(ImageChangeKind Kind, string? DataUrl = null, byte[]? Bytes = null, string? ContentType = null);

        private sealed record AppliedImageChange(string? NewKey, string? OldKey);

        /// <summary>
        /// Interprets the <c>image</c> field. Empty or "default.png" removes the photo; a data URL
        /// is a new photo; this motorcycle's own media URL (the edit form echoes back whatever the
        /// GET returned) means "unchanged". Anything else is rejected.
        /// </summary>
        private static ImagePlan PlanImage(string? image, Guid? existingId)
        {
            if (string.IsNullOrWhiteSpace(image) || image == "default.png")
                return new ImagePlan(ImageChangeKind.Remove);

            if (existingId is not null && IsOwnMediaUrl(image, existingId.Value))
                return new ImagePlan(ImageChangeKind.Keep);

            ImageDataUrlValidator.Validate(image, required: true);
            return new ImagePlan(ImageChangeKind.Replace, image, ImageDataUrlValidator.Decode(image), ImageDataUrlValidator.ContentTypeOf(image));
        }

        private static bool IsOwnMediaUrl(string image, Guid motorcycleId)
        {
            if (image.StartsWith("data:", StringComparison.Ordinal))
                return false;

            var path = image.Split('?', 2)[0];
            var isPathOrUrl = path.StartsWith('/') || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            return isPathOrUrl && path.EndsWith($"/api/media/motorcycles/{motorcycleId}", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<AppliedImageChange> ApplyImageAsync(Motorcycle entity, ImagePlan plan)
        {
            switch (plan.Kind)
            {
                case ImageChangeKind.Keep:
                    return new AppliedImageChange(null, null);

                case ImageChangeKind.Remove:
                    return new AppliedImageChange(null, ClearImage(entity));

                default:
                    await _quota.EnsureWithinAsync(entity.UserId, plan.Bytes!.Length, entity.ImageSizeBytes);

                    var oldKey = entity.ImageKey;
                    var version = Guid.NewGuid().ToString("N")[..12];
                    string? newKey = null;
                    if (_storage.IsEnabled)
                    {
                        // Upload first: a failed upload leaves the motorcycle untouched.
                        newKey = StorageKeys.ForMotorcycleImage(entity.UserId, entity.Id, version, plan.ContentType!);
                        await _storage.PutAsync(newKey, plan.Bytes, plan.ContentType!);
                        entity.Image = "default.png";
                    }
                    else
                    {
                        entity.Image = plan.DataUrl!;
                    }

                    entity.ImageKey = newKey;
                    entity.ImageVersion = version;
                    entity.ImageSizeBytes = plan.Bytes.Length;
                    return new AppliedImageChange(newKey, oldKey);
            }
        }

        /// <summary>Back to the default photo; returns the object key to delete once the change is saved.</summary>
        private static string? ClearImage(Motorcycle entity)
        {
            var oldKey = entity.ImageKey;
            entity.Image = "default.png";
            entity.ImageKey = null;
            entity.ImageVersion = null;
            entity.ImageSizeBytes = 0;
            return oldKey;
        }

        /// <summary>
        /// The photo as the API exposes it: a signed media URL whenever there is a photo
        /// (bucket or legacy data URL), so list payloads never carry base64.
        /// </summary>
        private MotorcycleDTO ToDto(Motorcycle entity)
        {
            var dto = entity.ToMotorcycleDTO();
            dto.Image = entity.ImageKey is not null || entity.Image.StartsWith("data:", StringComparison.Ordinal)
                ? _urlSigner.CreateMotorcycleUrl(entity.Id, entity.ImageVersion)
                : entity.Image;
            return dto;
        }

        // Best effort: a leftover object costs a few KB, a failed request costs the user.
        private async Task TryDeleteObjectsAsync(string[] keys)
        {
            try
            {
                await _storage.DeleteAsync(keys);
            }
            catch (Exception ex)
            {
                LogOrphanedObjects(_logger, keys.Length, ex);
            }
        }
    }
}
