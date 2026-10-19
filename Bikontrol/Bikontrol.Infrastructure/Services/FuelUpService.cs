using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
{
    public class FuelUpService : IFuelUpService
    {
        private readonly IFuelUpRepository _repo;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly ICurrentUserService _current;
        private readonly ITransactionManager _transactions;

        public FuelUpService(
            IFuelUpRepository repo,
            IMotorcycleRepository motorcycleRepository,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IKmHistoryService kmHistoryService,
            ICurrentUserService current,
            ITransactionManager transactions)
        {
            _repo = repo;
            _motorcycleRepository = motorcycleRepository;
            _recordRepository = recordRepository;
            _kmHistoryService = kmHistoryService;
            _current = current;
            _transactions = transactions;
        }

        public async Task<IEnumerable<FuelUpDTO>> GetByMotorcycleAsync(Guid motorcycleId, int? limit = null)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);
            var fuelUps = await _repo.GetByMotorcycleIdAsync(motorcycleId, limit);
            return fuelUps.ToFuelUpDTOs();
        }

        public async Task<FuelUpDTO?> GetByIdAsync(Guid id)
        {
            var fuelUp = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException("Carga de combustible no encontrada.");
            await EnsureMotorcycleOwnershipAsync(fuelUp.MotorcycleId);
            return fuelUp.ToFuelUpDTO();
        }

        public async Task<FuelUpDTO> CreateAsync(SaveFuelUpRequest request)
        {
            EnsureCanWrite();
            Validate(request);
            await EnsureMotorcycleOwnershipAsync(request.MotorcycleId);

            var entity = new FuelUp
            {
                MotorcycleId = request.MotorcycleId,
                FueledAt = request.FueledAt,
                OdometerKm = request.OdometerKm,
                Litres = request.Litres,
                TotalCost = request.TotalCost,
                IsFullTank = request.IsFullTank,
                Station = Normalize(request.Station),
                Notes = Normalize(request.Notes)
            };

            // Fuel-up + odometer advance in one transaction: a fuel-up is a natural
            // odometer checkpoint, so the maintenance countdown stays current.
            return await _transactions.ExecuteInTransactionAsync(async () =>
            {
                var created = await _repo.AddAsync(entity);
                await AdvanceOdometerIfNeededAsync(request.MotorcycleId, request.OdometerKm);
                await _repo.SaveChangesAsync();
                return created.ToFuelUpDTO();
            });
        }

        public async Task<FuelUpDTO> UpdateAsync(Guid id, SaveFuelUpRequest request)
        {
            EnsureCanWrite();
            Validate(request);

            var entity = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException("Carga de combustible no encontrada.");
            await EnsureMotorcycleOwnershipAsync(entity.MotorcycleId);
            await EnsureMotorcycleOwnershipAsync(request.MotorcycleId);

            entity.MotorcycleId = request.MotorcycleId;
            entity.FueledAt = request.FueledAt;
            entity.OdometerKm = request.OdometerKm;
            entity.Litres = request.Litres;
            entity.TotalCost = request.TotalCost;
            entity.IsFullTank = request.IsFullTank;
            entity.Station = Normalize(request.Station);
            entity.Notes = Normalize(request.Notes);

            return await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _repo.UpdateAsync(entity);
                await AdvanceOdometerIfNeededAsync(request.MotorcycleId, request.OdometerKm);
                await _repo.SaveChangesAsync();
                return entity.ToFuelUpDTO();
            });
        }

        public async Task DeleteAsync(Guid id)
        {
            EnsureCanWrite();

            var entity = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException("Carga de combustible no encontrada.");
            await EnsureMotorcycleOwnershipAsync(entity.MotorcycleId);

            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                _repo.Remove(entity);
                await _repo.SaveChangesAsync();
            });
        }

        public async Task<FuelConsumptionDTO> GetConsumptionAsync(Guid motorcycleId)
        {
            await EnsureMotorcycleOwnershipAsync(motorcycleId);

            var fuelUps = await _repo.GetByMotorcycleIdAsync(motorcycleId);
            var records = await _recordRepository.GetByMotorcycleIdAsync(motorcycleId);
            var maintenanceCost = records.Sum(r => r.Cost ?? 0m);

            return FuelConsumptionCalculator.Calculate(motorcycleId, fuelUps, maintenanceCost);
        }

        public async Task<IReadOnlyDictionary<Guid, FuelConsumptionDTO>> GetConsumptionByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds.Distinct().ToList();
            var result = new Dictionary<Guid, FuelConsumptionDTO>();
            if (ids.Count == 0)
                return result;

            await EnsureMotorcyclesOwnershipAsync(ids);

            var fuelUps = await _repo.GetByMotorcycleIdsAsync(ids);
            var records = await _recordRepository.GetByMotorcycleIdsAsync(ids);
            var maintenanceByMotorcycle = records
                .GroupBy(r => r.MotorcycleId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost ?? 0m));

            var fuelUpsByMotorcycle = fuelUps
                .GroupBy(f => f.MotorcycleId)
                .ToDictionary(g => g.Key, g => (IEnumerable<FuelUp>)g.ToList());

            foreach (var id in ids)
            {
                fuelUpsByMotorcycle.TryGetValue(id, out var motoFuelUps);
                var maintenanceCost = maintenanceByMotorcycle.TryGetValue(id, out var cost) ? cost : 0m;
                result[id] = FuelConsumptionCalculator.Calculate(id, motoFuelUps, maintenanceCost);
            }

            return result;
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }

        private static void Validate(SaveFuelUpRequest request)
        {
            if (request.FueledAt.Date > DateTime.UtcNow.Date)
                throw new ValidationException("No puedes registrar cargas posteriores al dia de hoy.");

            if (request.Litres <= 0)
                throw new ValidationException("Los litros deben ser mayores a cero.");

            if (request.TotalCost < 0)
                throw new ValidationException("El costo no puede ser negativo.");
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task AdvanceOdometerIfNeededAsync(Guid motorcycleId, int odometerKm)
        {
            // Only advance when the reading is above the current one; a backdated
            // entry below the odometer must not move it nor fail the operation.
            var currentKm = await _kmHistoryService.GetCurrentKmAsync(motorcycleId);
            if (odometerKm > currentKm)
                await _kmHistoryService.AddKmAsync(motorcycleId, odometerKm);
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
