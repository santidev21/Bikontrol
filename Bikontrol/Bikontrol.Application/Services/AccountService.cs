using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Account-level operations that span several repositories. Currently the
    /// "download my data" export; deletion will live here too.
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IUserMaintenanceRepository _userMaintenanceRepository;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IMaintenanceRecordAttachmentRepository _attachmentRepository;
        private readonly IFuelUpRepository _fuelUpRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITransactionManager _transactions;
        private readonly IObjectStorage _storage;
        private readonly ILogger<AccountService> _logger;
        private readonly ICurrentUserService _current;
        private readonly Microsoft.Extensions.Configuration.IConfiguration? _configuration;

        public AccountService(
            IUserRepository userRepository,
            IMotorcycleRepository motorcycleRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMaintenanceRecordAttachmentRepository attachmentRepository,
            IFuelUpRepository fuelUpRepository,
            IAccountRepository accountRepository,
            IKmHistoryService kmHistoryService,
            IPasswordHasher<User> passwordHasher,
            ITransactionManager transactions,
            IObjectStorage storage,
            ICurrentUserService current,
            Microsoft.Extensions.Configuration.IConfiguration? configuration = null,
            ILogger<AccountService>? logger = null)
        {
            _storage = storage;
            _logger = logger ?? NullLogger<AccountService>.Instance;
            _configuration = configuration;
            _userRepository = userRepository;
            _motorcycleRepository = motorcycleRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _recordRepository = recordRepository;
            _attachmentRepository = attachmentRepository;
            _fuelUpRepository = fuelUpRepository;
            _accountRepository = accountRepository;
            _kmHistoryService = kmHistoryService;
            _passwordHasher = passwordHasher;
            _transactions = transactions;
            _current = current;
        }

        public async Task<UserDataExportDTO> ExportMyDataAsync()
        {
            var user = await _userRepository.GetByIdAsync(_current.UserId)
                ?? throw new NotFoundException("Usuario no encontrado.", "user_not_found");

            var motorcycles = (await _motorcycleRepository.GetByUserIdAsync(_current.UserId)).ToList();
            var motorcycleIds = motorcycles.Select(m => m.Id).ToList();

            var maintenances = (await _userMaintenanceRepository.GetByUserIdAsync(_current.UserId)).ToList();
            var records = motorcycleIds.Count == 0
                ? new List<MotorcycleMaintenanceRecord>()
                : (await _recordRepository.GetByMotorcycleIdsAsync(motorcycleIds)).ToList();

            var attachments = records.Count == 0
                ? new List<MaintenanceRecordAttachment>()
                : (await _attachmentRepository.GetByRecordIdsAsync(records.Select(r => r.Id))).ToList();

            var fuelUps = motorcycleIds.Count == 0
                ? new List<FuelUp>()
                : (await _fuelUpRepository.GetByMotorcycleIdsAsync(motorcycleIds)).ToList();

            var currentKms = motorcycleIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await _kmHistoryService.GetCurrentKmByMotorcycleIdsAsync(motorcycleIds);

            var motorcycleDtos = motorcycles.ToMotorcycleDTOs();
            var imageKeys = motorcycles.Where(m => m.ImageKey is not null).ToDictionary(m => m.Id, m => m.ImageKey!);
            foreach (var dto in motorcycleDtos)
            {
                // MotorcycleDTO.Km is resolved from the km history, not the entity.
                dto.Km = currentKms.TryGetValue(dto.Id, out var km) ? km : 0;

                // Photos in the bucket are inlined (a signed URL would expire inside the file).
                if (imageKeys.TryGetValue(dto.Id, out var imageKey))
                {
                    dto.Image = await ReadAsDataUrlAsync(imageKey, StorageKeys.ContentTypeOfKey(imageKey), dto.Id) ?? "default.png";
                }
            }

            var maintenanceNames = maintenances.ToDictionary(m => m.Id, m => m.Name);
            var recordDtos = records.ToMaintenanceRecordDTOs();
            foreach (var dto in recordDtos)
            {
                if (maintenanceNames.TryGetValue(dto.UserMaintenanceId, out var name))
                {
                    dto.MaintenanceName = name;
                }
            }

            return new UserDataExportDTO
            {
                ExportedAt = DateTime.UtcNow,
                Profile = ToProfile(user),
                Motorcycles = motorcycleDtos,
                Maintenances = maintenances.ToMaintenanceDTOs(),
                MaintenanceRecords = recordDtos,
                Attachments = await ToExportAttachmentsAsync(attachments),
                FuelUps = fuelUps.ToFuelUpDTOs().ToList(),
            };
        }

        public async Task DeleteMyAccountAsync(DeleteAccountRequest request)
        {
            // The demo tenant is shared: nobody may delete it.
            if (_current.IsDemo)
            {
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.", "demo_read_only");
            }

            if (!string.Equals(request.Confirmation?.Trim(), "ELIMINAR", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Escribe ELIMINAR para confirmar.", "delete_confirmation_required");
            }

            var user = await _userRepository.GetByIdAsync(_current.UserId)
                ?? throw new NotFoundException("Usuario no encontrado.", "user_not_found");

            if (user.HasPassword)
            {
                PasswordAttemptGuard.EnsureNotLocked(user, _configuration);
                if (string.IsNullOrWhiteSpace(request.Password)
                    || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                        != PasswordVerificationResult.Success)
                {
                    PasswordAttemptGuard.RegisterFailure(user, _configuration);
                    await _userRepository.SaveChangesAsync();
                    throw new ValidationException("La contraseña no es correcta.", "current_password_incorrect");
                }
            }

            // Collect the object keys first: the purge below deletes the rows that name them.
            var storageKeys = (await _attachmentRepository.GetStorageKeysByUserAsync(user.Id))
                .Concat(await _motorcycleRepository.GetImageKeysByUserAsync(user.Id))
                .ToList();

            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _accountRepository.PurgeUserDataAsync(user.Id);
                user.Anonymize();
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            });

            // After the commit, so a rolled-back deletion never loses the user's images.
            // Best effort: an object left behind is unreachable (no row points to it).
            if (storageKeys.Count > 0)
            {
                try
                {
                    await _storage.DeleteAsync(storageKeys);
                }
                catch (Exception ex)
                {
                    LogObjectsNotRemoved(_logger, storageKeys.Count, ex);
                }
            }
        }

        // The export is a real copy of the user's data: stored images are inlined as
        // data URLs (signed URLs would expire inside the downloaded file).
        private async Task<List<MaintenanceAttachmentDTO>> ToExportAttachmentsAsync(IEnumerable<MaintenanceRecordAttachment> attachments)
        {
            var result = new List<MaintenanceAttachmentDTO>();
            foreach (var attachment in attachments)
            {
                var dto = attachment.ToAttachmentDTO();
                if (attachment.StorageKey is not null)
                {
                    dto.DataUrl = await ReadAsDataUrlAsync(attachment.StorageKey, attachment.ContentType, attachment.Id) ?? string.Empty;
                }

                result.Add(dto);
            }

            return result;
        }

        /// <summary>The stored object as a data URL, or null (logged) when the key points at nothing.</summary>
        private async Task<string?> ReadAsDataUrlAsync(string key, string contentType, Guid ownerEntityId)
        {
            var stored = await _storage.GetAsync(key);
            if (stored is null)
            {
                LogMissingObject(_logger, ownerEntityId, null);
                return null;
            }

            using var buffer = new MemoryStream();
            await stored.Content.CopyToAsync(buffer);
            stored.Dispose();
            return ImageDataUrlValidator.Encode(buffer.ToArray(), contentType);
        }

        private ProfileDTO ToProfile(User user)
        {
            var dto = user.ToProfileDTO();
            dto.HasPassword = user.HasPassword;
            return dto;
        }

        private static readonly Action<ILogger, int, Exception?> LogObjectsNotRemoved = LoggerMessage.Define<int>(
            LogLevel.Warning, new EventId(1, nameof(LogObjectsNotRemoved)), "Account deleted but {Count} stored object(s) could not be removed.");

        private static readonly Action<ILogger, Guid, Exception?> LogMissingObject = LoggerMessage.Define<Guid>(
            LogLevel.Warning, new EventId(2, nameof(LogMissingObject)), "Stored image of {EntityId} points to a missing object; exporting it empty.");
    }
}
