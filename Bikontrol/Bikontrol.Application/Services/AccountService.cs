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
        private readonly ICurrentUserService _current;

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
            ICurrentUserService current)
        {
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
            foreach (var dto in motorcycleDtos)
            {
                // MotorcycleDTO.Km is resolved from the km history, not the entity.
                dto.Km = currentKms.TryGetValue(dto.Id, out var km) ? km : 0;
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
                Attachments = attachments.ToAttachmentDTOs(),
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
                if (string.IsNullOrWhiteSpace(request.Password)
                    || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                        != PasswordVerificationResult.Success)
                {
                    throw new ValidationException("La contraseña no es correcta.", "current_password_incorrect");
                }
            }

            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _accountRepository.PurgeUserDataAsync(user.Id);
                user.Anonymize();
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            });
        }

        private ProfileDTO ToProfile(User user)
        {
            var dto = user.ToProfileDTO();
            dto.HasPassword = user.HasPassword;
            return dto;
        }
    }
}
