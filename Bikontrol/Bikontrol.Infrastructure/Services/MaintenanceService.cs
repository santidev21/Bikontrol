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
        private readonly ITransactionManager _transactions;

        public MaintenanceService(
            IMaintenanceRepository repo,
            IUserMaintenanceRepository userRepo,
            IMotorcycleRepository motorcycleRepository,
            IKmHistoryService kmHistoryService,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IMapper mapper,
            ICurrentUserService current,
            ITransactionManager transactions)
        {
            _repo = repo;
            _userRepo = userRepo;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _mapper = mapper;
            _current = current;
            _transactions = transactions;
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

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }

        private static void EnsureValidInterval(string trackingType, int? kmInterval, int? timeIntervalWeeks)
        {
            if (trackingType != "Km" && trackingType != "Time")
                throw new ValidationException("El tipo de seguimiento debe ser 'Km' o 'Time'.");

            if (trackingType == "Km" && (!kmInterval.HasValue || kmInterval.Value <= 0))
                throw new ValidationException("Debes indicar un intervalo de kilometraje mayor a cero.");

            if (trackingType == "Time" && (!timeIntervalWeeks.HasValue || timeIntervalWeeks.Value <= 0))
                throw new ValidationException("Debes indicar un intervalo de tiempo mayor a cero.");
        }

        public async Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto)
        {
            EnsureCanWrite();
            EnsureValidInterval(dto.TrackingType, dto.KmInterval, dto.TimeIntervalWeeks);
            await EnsureMotorcycleOwnershipAsync(dto.MotorcycleId);

            var entity = _mapper.Map<UserMaintenance>(dto);
            entity.UserId = _current.UserId;
            entity.MotorcycleId = dto.MotorcycleId;

            var created = await _userRepo.AddAsync(entity);
            return _mapper.Map<MaintenanceDTO>(created);
        }

        public async Task DeleteUserMaintenanceAsync(Guid id)
        {
            EnsureCanWrite();
            var entity = await _userRepo.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.");
            if (entity.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar este mantenimiento.");

            await _userRepo.SoftDeleteAsync(id);
        }

        public async Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? kmInterval, int? timeIntervalWeeks, string trackingType)
        {
            EnsureCanWrite();
            EnsureValidInterval(trackingType, kmInterval, timeIntervalWeeks);
            await EnsureMotorcycleOwnershipAsync(motorcycleId);

            var defaultEntity = await _repo.GetByIdAsync(defaultId);
            if (defaultEntity == null)
                throw new NotFoundException("No se encontró el mantenimiento predeterminado.");

            var existing = await _userRepo.GetByBaseIdAsync(_current.UserId, motorcycleId, defaultId);
            if (existing != null)
            {
                if (existing.IsEnabled) throw new ValidationException("Ya estás siguiendo este mantenimiento.");
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
            EnsureCanWrite();
            EnsureValidInterval(dto.TrackingType, dto.KmInterval, dto.TimeIntervalWeeks);
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
            EnsureCanWrite();
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

            // Registro + avance de odómetro en una sola transacción: antes eran
            // dos SaveChanges separados y un km inválido dejaba el registro
            // guardado sin su km (escritura parcial).
            return await _transactions.ExecuteInTransactionAsync(async () =>
            {
                var created = await _recordRepository.AddAsync(record);

                if (request.PerformedKm.HasValue)
                {
                    // Solo avanza el odómetro si el km registrado es mayor al actual.
                    // Un registro histórico (km menor al odómetro) no debe moverlo
                    // ni romper la operación.
                    var currentKm = await _kmHistoryService.GetCurrentKmAsync(request.MotorcycleId);
                    if (request.PerformedKm.Value > currentKm)
                        await _kmHistoryService.AddKmAsync(request.MotorcycleId, request.PerformedKm.Value);
                }

                var dto = _mapper.Map<MaintenanceRecordDTO>(created);
                dto.MaintenanceName = maintenance.Name;
                return dto;
            });
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
            var maintenances = (await _userRepo.GetByUserIdAndMotorcycleIdAsync(_current.UserId, motorcycleId)).ToList();

            // Último registro por mantenimiento en una sola consulta (antes era N+1).
            var lastRecords = await _recordRepository.GetLastByUserMaintenanceIdsAsync(maintenances.Select(m => m.Id));

            return maintenances
                .Select(maintenance => CalculateUpcoming(
                    maintenance,
                    lastRecords.TryGetValue(maintenance.Id, out var last) ? last : null,
                    currentKm,
                    initialRecordedAt))
                .OrderBy(x => x.LifePercent)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<UpcomingMaintenanceDTO>>> GetUpcomingByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            var result = new Dictionary<Guid, IReadOnlyList<UpcomingMaintenanceDTO>>();
            if (ids.Count == 0)
                return result;

            await EnsureMotorcyclesOwnershipAsync(ids);

            var maintenances = (await _userRepo.GetByUserIdAsync(_current.UserId))
                .Where(m => ids.Contains(m.MotorcycleId))
                .ToList();
            var lastRecords = await _recordRepository.GetLastByUserMaintenanceIdsAsync(maintenances.Select(m => m.Id));
            var currentKms = await _kmHistoryService.GetCurrentKmByMotorcycleIdsAsync(ids);
            var initialDates = await _kmHistoryService.GetInitialRecordedAtByMotorcycleIdsAsync(ids);

            foreach (var id in ids)
            {
                var currentKm = currentKms.TryGetValue(id, out var km) ? km : 0;
                initialDates.TryGetValue(id, out var initialRecordedAt);

                result[id] = maintenances
                    .Where(m => m.MotorcycleId == id)
                    .Select(m => CalculateUpcoming(
                        m,
                        lastRecords.TryGetValue(m.Id, out var last) ? last : null,
                        currentKm,
                        initialRecordedAt))
                    .OrderBy(x => x.LifePercent)
                    .ThenBy(x => x.Name)
                    .ToList();
            }

            return result;
        }

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MaintenanceRecordDTO>>> GetMaintenanceRecordsByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            var result = new Dictionary<Guid, IReadOnlyList<MaintenanceRecordDTO>>();
            if (ids.Count == 0)
                return result;

            await EnsureMotorcyclesOwnershipAsync(ids);

            var mapped = _mapper.Map<List<MaintenanceRecordDTO>>(await _recordRepository.GetByMotorcycleIdsAsync(ids));
            foreach (var id in ids)
            {
                result[id] = mapped.Where(r => r.MotorcycleId == id).ToList();
            }

            return result;
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

        private async Task EnsureMotorcyclesOwnershipAsync(IReadOnlyCollection<Guid> motorcycleIds)
        {
            var owned = (await _motorcycleRepository.GetByUserIdAsync(_current.UserId))
                .Select(m => m.Id)
                .ToHashSet();

            if (motorcycleIds.Any(id => !owned.Contains(id)))
                throw new ForbiddenAccessException("No tienes permisos para ver estas motocicletas.");
        }
    }
}
