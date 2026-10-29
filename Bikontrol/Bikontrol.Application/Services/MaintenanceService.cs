using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Application.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _repo;
        private readonly IUserMaintenanceRepository _userRepo;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly ICurrentUserService _current;
        private readonly ITransactionManager _transactions;

        public MaintenanceService(
            IMaintenanceRepository repo,
            IUserMaintenanceRepository userRepo,
            IMotorcycleRepository motorcycleRepository,
            IKmHistoryService kmHistoryService,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            ICurrentUserService current,
            ITransactionManager transactions)
        {
            _repo = repo;
            _userRepo = userRepo;
            _motorcycleRepository = motorcycleRepository;
            _kmHistoryService = kmHistoryService;
            _recordRepository = recordRepository;
            _current = current;
            _transactions = transactions;
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync()
        {
            var list = await _repo.GetAllForUserAsync(_current.UserId);
            return list.ToMaintenanceDTOs();
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync()
        {
            var list = await _userRepo.GetByUserIdAsync(_current.UserId);
            return list.ToMaintenanceDTOs();
        }

        public async Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceByMotorcycleAsync(Guid motorcycleId)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);
            var list = await _userRepo.GetByUserIdAndMotorcycleIdAsync(_current.UserId, motorcycleId);
            return list.ToMaintenanceDTOs();
        }

        public async Task<MaintenanceDTO?> GetByIdAsync(Guid id)
        {
            var maintenance = await _userRepo.GetByIdAsync(id);
            if (maintenance is null) throw new NotFoundException("Mantenimiento no encontrado.", "maintenance_not_found");
            if (maintenance.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para ver este mantenimiento.", "maintenance_no_access");

            return maintenance.ToMaintenanceDTO();
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.", "demo_read_only");
        }

        private static void EnsureValidInterval(string trackingType, int? kmInterval, int? timeIntervalWeeks)
        {
            if (trackingType != "Km" && trackingType != "Time")
                throw new ValidationException("El tipo de seguimiento debe ser 'Km' o 'Time'.", "tracking_type_invalid");

            if (trackingType == "Km" && (!kmInterval.HasValue || kmInterval.Value <= 0))
                throw new ValidationException("Debes indicar un intervalo de kilometraje mayor a cero.", "km_interval_required");

            if (trackingType == "Time" && (!timeIntervalWeeks.HasValue || timeIntervalWeeks.Value <= 0))
                throw new ValidationException("Debes indicar un intervalo de tiempo mayor a cero.", "time_interval_required");
        }

        public async Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto)
        {
            EnsureCanWrite();
            EnsureValidInterval(dto.TrackingType, dto.KmInterval, dto.TimeIntervalWeeks);
            await EnsureMotorcycleOwnershipAsync(dto.MotorcycleId);

            var entity = new UserMaintenance
            {
                UserId = _current.UserId,
                MotorcycleId = dto.MotorcycleId,
                Name = dto.Name,
                Description = dto.Description,
                KmInterval = dto.KmInterval,
                TimeIntervalWeeks = dto.TimeIntervalWeeks,
                TrackingType = dto.TrackingType
            };

            var created = await _userRepo.AddAsync(entity);
            await _userRepo.SaveChangesAsync();
            return created.ToMaintenanceDTO();
        }

        public async Task DeleteUserMaintenanceAsync(Guid id)
        {
            EnsureCanWrite();
            var entity = await _userRepo.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.", "maintenance_not_found");
            if (entity.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para borrar este mantenimiento.", "maintenance_no_access");

            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _userRepo.SoftDeleteAsync(id);
                await _userRepo.SaveChangesAsync();
            });
        }

        public async Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? kmInterval, int? timeIntervalWeeks, string trackingType)
        {
            EnsureCanWrite();
            EnsureValidInterval(trackingType, kmInterval, timeIntervalWeeks);
            await EnsureMotorcycleOwnershipAsync(motorcycleId);

            var defaultEntity = await _repo.GetByIdAsync(defaultId);
            if (defaultEntity == null)
                throw new NotFoundException("No se encontró el mantenimiento predeterminado.", "default_maintenance_not_found");

            var existing = await _userRepo.GetByBaseIdAsync(_current.UserId, motorcycleId, defaultId);
            if (existing != null)
            {
                if (existing.IsEnabled) throw new ValidationException("Ya estás siguiendo este mantenimiento.", "already_following");
                existing.IsEnabled = true;
                existing.KmInterval = kmInterval;
                existing.TimeIntervalWeeks = timeIntervalWeeks;
                existing.TrackingType = trackingType;
                await _userRepo.UpdateAsync(existing);
                await _userRepo.SaveChangesAsync();
                return existing.ToMaintenanceDTO();
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
            await _userRepo.SaveChangesAsync();
            return created.ToMaintenanceDTO();
        }

        public async Task UpdateAsync(Guid id, SaveMaintenanceDTO dto)
        {
            EnsureCanWrite();
            EnsureValidInterval(dto.TrackingType, dto.KmInterval, dto.TimeIntervalWeeks);
            var entity = await _userRepo.GetByIdAsync(id);
            if (entity is null) throw new NotFoundException("Mantenimiento no encontrado.", "maintenance_not_found");
            if (entity.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para editar este mantenimiento.", "maintenance_no_access");

            await EnsureMotorcycleOwnershipAsync(dto.MotorcycleId);

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.KmInterval = dto.KmInterval;
            entity.TimeIntervalWeeks = dto.TimeIntervalWeeks;
            entity.TrackingType = dto.TrackingType;
            entity.MotorcycleId = dto.MotorcycleId;
            await _userRepo.UpdateAsync(entity);
            await _userRepo.SaveChangesAsync();
        }

        public async Task<MaintenanceRecordDTO> RegisterMaintenanceRecordAsync(CreateMaintenanceRecordRequest request)
        {
            EnsureCanWrite();
            if (request.PerformedAt.Date > DateTime.UtcNow.Date)
                throw new ValidationException("No puedes agregar mantenimientos posteriores al dia de hoy", "maintenance_future_date");

            var maintenance = await _userRepo.GetByIdAsync(request.UserMaintenanceId);
            if (maintenance is null) throw new NotFoundException("Mantenimiento no encontrado.", "maintenance_not_found");
            if (maintenance.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para registrar este mantenimiento.", "maintenance_no_access");
            if (maintenance.MotorcycleId != request.MotorcycleId)
                throw new ValidationException("El mantenimiento no pertenece a la motocicleta seleccionada.", "maintenance_not_on_motorcycle");

            await EnsureMotorcycleOwnershipAsync(request.MotorcycleId);

            if (maintenance.TrackingType == "Km" && !request.PerformedKm.HasValue)
                throw new ValidationException("Debes ingresar kilometraje para este mantenimiento.", "km_required");

            // Monotonic km: a record must not be lower than the one before it nor
            // higher than the one after it (by PerformedAt), otherwise the history
            // becomes inconsistent (integrity audit check 5). Compare against the
            // neighbours around the new date, not just the latest record, so a
            // backdated insert cannot slip past.
            if (request.PerformedKm.HasValue)
            {
                var siblings = (await _recordRepository.GetByMotorcycleIdAsync(maintenance.MotorcycleId))
                    .Where(r => r.UserMaintenanceId == maintenance.Id && r.PerformedKm.HasValue)
                    .ToList();

                var previous = siblings
                    .Where(r => r.PerformedAt <= request.PerformedAt)
                    .OrderByDescending(r => r.PerformedAt)
                    .ThenByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                if (previous is not null && request.PerformedKm.Value < previous.PerformedKm!.Value)
                    throw new ValidationException("El kilometraje no puede ser menor al del registro anterior de este mantenimiento.", "km_below_previous");

                var next = siblings
                    .Where(r => r.PerformedAt > request.PerformedAt)
                    .OrderBy(r => r.PerformedAt)
                    .ThenBy(r => r.CreatedAt)
                    .FirstOrDefault();
                if (next is not null && request.PerformedKm.Value > next.PerformedKm!.Value)
                    throw new ValidationException("El kilometraje no puede ser mayor al del registro posterior de este mantenimiento.", "km_above_next");
            }

            var record = new MotorcycleMaintenanceRecord
            {
                MotorcycleId = request.MotorcycleId,
                UserMaintenanceId = maintenance.Id,
                PerformedAt = request.PerformedAt,
                PerformedKm = request.PerformedKm,
                Cost = request.Cost
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

                var dto = created.ToMaintenanceRecordDTO();
                dto.MaintenanceName = maintenance.Name;
                return dto;
            });
        }

        public async Task<IEnumerable<MaintenanceRecordDTO>> GetMaintenanceRecordsByMotorcycleAsync(Guid motorcycleId, int? limit = null)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);
            var records = await _recordRepository.GetByMotorcycleIdAsync(motorcycleId, limit);
            return records.ToMaintenanceRecordDTOs();
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

            var mapped = (await _recordRepository.GetByMotorcycleIdsAsync(ids)).ToMaintenanceRecordDTOs();
            foreach (var id in ids)
            {
                result[id] = mapped.Where(r => r.MotorcycleId == id).ToList();
            }

            return result;
        }

        // Shared with the reminder engine so the countdown cannot drift.
        private static UpcomingMaintenanceDTO CalculateUpcoming(UserMaintenance maintenance, MotorcycleMaintenanceRecord? lastRecord, int currentKm, DateTime? initialRecordedAt)
            => MaintenanceScheduleCalculator.Calculate(maintenance, lastRecord, currentKm, initialRecordedAt);

        private Task EnsureMotorcycleOwnershipAsync(Guid motorcycleId)
            => MotorcycleOwnership.EnsureOwnedAsync(_motorcycleRepository, _current.UserId, motorcycleId);

        private Task EnsureMotorcyclesOwnershipAsync(IReadOnlyCollection<Guid> motorcycleIds)
            => MotorcycleOwnership.EnsureAllOwnedAsync(_motorcycleRepository, _current.UserId, motorcycleIds);
    }
}
