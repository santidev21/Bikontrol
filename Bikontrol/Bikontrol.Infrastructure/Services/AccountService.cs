using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
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
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _current;

        public AccountService(
            IUserRepository userRepository,
            IMotorcycleRepository motorcycleRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMaintenanceRecordAttachmentRepository attachmentRepository,
            IKmHistoryService kmHistoryService,
            IMapper mapper,
            ICurrentUserService current)
        {
            _userRepository = userRepository;
            _motorcycleRepository = motorcycleRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _recordRepository = recordRepository;
            _attachmentRepository = attachmentRepository;
            _kmHistoryService = kmHistoryService;
            _mapper = mapper;
            _current = current;
        }

        public async Task<UserDataExportDTO> ExportMyDataAsync()
        {
            var user = await _userRepository.GetByIdAsync(_current.UserId)
                ?? throw new NotFoundException("Usuario no encontrado.");

            var motorcycles = (await _motorcycleRepository.GetByUserIdAsync(_current.UserId)).ToList();
            var motorcycleIds = motorcycles.Select(m => m.Id).ToList();

            var maintenances = (await _userMaintenanceRepository.GetByUserIdAsync(_current.UserId)).ToList();
            var records = motorcycleIds.Count == 0
                ? new List<MotorcycleMaintenanceRecord>()
                : (await _recordRepository.GetByMotorcycleIdsAsync(motorcycleIds)).ToList();

            var attachments = records.Count == 0
                ? new List<MaintenanceRecordAttachment>()
                : (await _attachmentRepository.GetByRecordIdsAsync(records.Select(r => r.Id))).ToList();

            var currentKms = motorcycleIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await _kmHistoryService.GetCurrentKmByMotorcycleIdsAsync(motorcycleIds);

            var motorcycleDtos = _mapper.Map<List<MotorcycleDTO>>(motorcycles);
            foreach (var dto in motorcycleDtos)
            {
                // MotorcycleDTO.Km is resolved from the km history, not the entity.
                dto.Km = currentKms.TryGetValue(dto.Id, out var km) ? km : 0;
            }

            var maintenanceNames = maintenances.ToDictionary(m => m.Id, m => m.Name);
            var recordDtos = _mapper.Map<List<MaintenanceRecordDTO>>(records);
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
                Maintenances = _mapper.Map<List<MaintenanceDTO>>(maintenances),
                MaintenanceRecords = recordDtos,
                Attachments = _mapper.Map<List<MaintenanceAttachmentDTO>>(attachments),
            };
        }

        private ProfileDTO ToProfile(User user)
        {
            var dto = _mapper.Map<ProfileDTO>(user);
            dto.HasPassword = user.HasPassword;
            return dto;
        }
    }
}
