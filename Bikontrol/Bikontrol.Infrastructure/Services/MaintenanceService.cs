using AutoMapper;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _repo;
        private readonly IUserMaintenanceRepository _userRepo;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _current;

        public MaintenanceService(
            IMaintenanceRepository repo,
            IUserMaintenanceRepository userRepo,
            IMotorcycleRepository motorcycleRepository,
            IKmHistoryService kmHistoryService,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMapper mapper,
            ICurrentUserService current)
        {
            _repo = repo;
            _userRepo = userRepo;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _mapper = mapper;
            _current = current;
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync()
        {
            var list = await _repo.GetAllForUserAsync(_current.UserId);
            return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync()
        {
            var list = await _userRepo.GetByUserIdAsync(_current.UserId);
            return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceByMotorcycleAsync(Guid motorcycleId)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);
            var list = await _userRepo.GetByUserIdAndMotorcycleIdAsync(_current.UserId, motorcycleId);
            return _mapper.Map<IEnumerable<MaintenanceDTO>>(list);
        }

        public async Task<MaintenanceDTO?> GetByIdAsync(Guid id)
        {
            var maintenance = await _userRepo.GetByIdAsync(id);
            if (maintenance is null) throw new NotFoundException("Mantenimiento no encontrado.");
            if (maintenance.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para ver este mantenimiento.");

            return _mapper.Map<MaintenanceDTO>(maintenance);
        }

        public async Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto)
        {
            await EnsureMotorcycleOwnershipAsync(dto.MotorcycleId);

            var entity = _mapper.Map<UserMaintenance>(dto);
            entity.UserId = _current.UserId;
            entity.MotorcycleId = dto.MotorcycleId;

            var created = await _userRepo.AddAsync(entity);
            return _mapper.Map<MaintenanceDTO>(created);
        }

        public async Task DeleteUserMaintenanceAsync(Guid id)
        {
            var entity = await _userRepo.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.");
            if (entity.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar este mantenimiento.");

            await _userRepo.SoftDeleteAsync(id);
        }

        public async Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? kmInterval, int? timeIntervalWeeks, string trackingType)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);

            var defaultEntity = await _repo.GetByIdAsync(defaultId);
            if (defaultEntity == null)
                throw new NotFoundException("Default maintenance type not found.");

            var existing = await _userRepo.GetByBaseIdAsync(_current.UserId, motorcycleId, defaultId);
            if (existing != null)
            {
                if (existing.IsEnabled) throw new ValidationException("You are already following this maintenance.");
                existing.IsEnabled = true;
                existing.KmInterval = kmInterval;
                existing.TimeIntervalWeeks = timeIntervalWeeks;
                existing.TrackingType = trackingType;
                await _userRepo.UpdateAsync(existing);
                return _mapper.Map<MaintenanceDTO>(existing);
            }

            var entity = new UserMaintenance
            {
                UserId = _current.UserId,
                MotorcycleId = motorcycleId,
                BaseTypeId = defaultEntity.Id,
                Name = defaultEntity.Name,
                Description = defaultEntity.Description,
                KmInterval = kmInterval,
                TimeIntervalWeeks = timeIntervalWeeks,
                TrackingType = trackingType,
                IsEnabled = true
            };

            var created = await _userRepo.AddAsync(entity);
            return _mapper.Map<MaintenanceDTO>(created);
        }

        public async Task UpdateAsync(Guid id, SaveMaintenanceDTO dto)
        {
            var entity = await _userRepo.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.");
            if (entity.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar este mantenimiento.");

            await EnsureMotorcycleOwnershipAsync(dto.MotorcycleId);

            _mapper.Map(dto, entity);
            entity.MotorcycleId = dto.MotorcycleId;
            await _userRepo.UpdateAsync(entity);
        }

        public async Task<MaintenanceRecordDTO> RegisterMaintenanceRecordAsync(CreateMaintenanceRecordRequest request)
        {
            if (request.PerformedAt.Date > DateTime.UtcNow.Date)
                throw new ValidationException("No puedes agregar mantenimientos posteriores al dia de hoy");

            var maintenance = await _userRepo.GetByIdAsync(request.UserMaintenanceId);
            if (maintenance is null) throw new NotFoundException("Mantenimiento no encontrado.");
            if (maintenance.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para registrar este mantenimiento.");
            if (maintenance.MotorcycleId != request.MotorcycleId)
                throw new ValidationException("El mantenimiento no pertenece a la motocicleta seleccionada.");

            await EnsureMotorcycleOwnershipAsync(request.MotorcycleId);

            if (maintenance.TrackingType == "Km" && !request.PerformedKm.HasValue)
                throw new ValidationException("Debes ingresar kilometraje para este mantenimiento.");

            var lastMaintenanceRecord = await _recordRepository.GetLastByUserMaintenanceIdAsync(maintenance.Id);
            if (request.PerformedKm.HasValue)
            {
                if (lastMaintenanceRecord?.PerformedKm is int lastKm && request.PerformedKm.Value < lastKm)
                    throw new ValidationException("No puedes agregar mantenimiento anterior al ultimo");
            }

            var record = new MotorcycleMaintenanceRecord
            {
                MotorcycleId = request.MotorcycleId,
                UserMaintenanceId = maintenance.Id,
                PerformedAt = request.PerformedAt,
                PerformedKm = request.PerformedKm
            };

            var created = await _recordRepository.AddAsync(record);

            if (request.PerformedKm.HasValue)
                await _kmHistoryService.AddKmAsync(request.MotorcycleId, request.PerformedKm.Value);

            var dto = _mapper.Map<MaintenanceRecordDTO>(created);
            dto.MaintenanceName = maintenance.Name;
            return dto;
        }

        public async Task<IEnumerable<MaintenanceRecordDTO>> GetMaintenanceRecordsByMotorcycleAsync(Guid motorcycleId)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);
            var records = await _recordRepository.GetByMotorcycleIdAsync(motorcycleId);
            return _mapper.Map<IEnumerable<MaintenanceRecordDTO>>(records);
        }

        public async Task<IEnumerable<UpcomingMaintenanceDTO>> GetUpcomingByMotorcycleAsync(Guid motorcycleId)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);

            var currentKm = await _kmHistoryService.GetCurrentKmAsync(motorcycleId);
            var initialRecordedAt = await _kmHistoryService.GetInitialRecordedAtAsync(motorcycleId);
            var maintenances = await _userRepo.GetByUserIdAndMotorcycleIdAsync(_current.UserId, motorcycleId);

            var result = new List<UpcomingMaintenanceDTO>();

            foreach (var maintenance in maintenances)
            {
                var lastRecord = await _recordRepository.GetLastByUserMaintenanceIdAsync(maintenance.Id);
                result.Add(CalculateUpcoming(maintenance, lastRecord, currentKm, initialRecordedAt));
            }

            return result
                .OrderBy(x => x.LifePercent)
                .ThenBy(x => x.Name)
                .ToList();
        }

        private UpcomingMaintenanceDTO CalculateUpcoming(UserMaintenance maintenance, MotorcycleMaintenanceRecord? lastRecord, int currentKm, DateTime? initialRecordedAt)
        {
            var dto = new UpcomingMaintenanceDTO
            {
                UserMaintenanceId = maintenance.Id,
                MotorcycleId = maintenance.MotorcycleId,
                Name = maintenance.Name,
                Description = maintenance.Description,
                TrackingType = maintenance.TrackingType,
                KmInterval = maintenance.KmInterval,
                TimeIntervalWeeks = maintenance.TimeIntervalWeeks,
                LastPerformedAt = lastRecord?.PerformedAt,
                LastPerformedKm = lastRecord?.PerformedKm
            };

            if (maintenance.TrackingType == "Km")
            {
                var interval = maintenance.KmInterval ?? 0;
                if (interval <= 0)
                {
                    dto.LifePercent = 0;
                    dto.RemainingKm = 0;
                    dto.IsOverdue = true;
                    return dto;
                }

                var baselineKm = lastRecord?.PerformedKm ?? 0;
                var used = currentKm - baselineKm;
                var remaining = interval - used;
                var life = (int)Math.Floor((double)remaining * 100 / interval);

                dto.RemainingKm = remaining;
                dto.RemainingDays = 0;
                dto.IsOverdue = remaining <= 0;
                dto.LifePercent = Math.Clamp(life, 0, 100);
                return dto;
            }

            var intervalDays = (maintenance.TimeIntervalWeeks ?? 0) * 7;
            if (intervalDays <= 0)
            {
                dto.LifePercent = 0;
                dto.RemainingDays = 0;
                dto.IsOverdue = true;
                return dto;
            }

            if (lastRecord is null && initialRecordedAt is null)
            {
                dto.LifePercent = 0;
                dto.RemainingDays = -intervalDays;
                dto.IsOverdue = true;
                return dto;
            }

            var baselineDate = lastRecord?.PerformedAt.Date ?? initialRecordedAt!.Value.Date;
            dto.LastPerformedAt = baselineDate;
            var daysUsed = (DateTime.UtcNow.Date - baselineDate).Days;
            var remainingDays = intervalDays - daysUsed;
            var timeLife = (int)Math.Floor((double)remainingDays * 100 / intervalDays);

            dto.RemainingDays = remainingDays;
            dto.RemainingKm = 0;
            dto.IsOverdue = remainingDays <= 0;
            dto.LifePercent = Math.Clamp(timeLife, 0, 100);
            return dto;
        }

        private async Task EnsureMotorcycleOwnershipAsync(Guid motorcycleId)
        {
            var motorcycle = await _motorcycleRepository.GetByIdAsync(motorcycleId);
            if (motorcycle is null) throw new NotFoundException("Motocicleta no encontrada.");
            if (motorcycle.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para esta motocicleta.");
        }
    }
}
