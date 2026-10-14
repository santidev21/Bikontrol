using Bikontrol.Application.DTOs.Fuel;

namespace Bikontrol.Application.Interfaces
{
    public interface IFuelUpService
    {
        Task<IEnumerable<FuelUpDTO>> GetByMotorcycleAsync(Guid motorcycleId);

        Task<FuelUpDTO?> GetByIdAsync(Guid id);

        Task<FuelUpDTO> CreateAsync(SaveFuelUpRequest request);

        Task<FuelUpDTO> UpdateAsync(Guid id, SaveFuelUpRequest request);

        Task DeleteAsync(Guid id);

        /// <summary>Consumption and cost metrics for one motorcycle.</summary>
        Task<FuelConsumptionDTO> GetConsumptionAsync(Guid motorcycleId);

        /// <summary>
        /// Consumption and cost metrics for several motorcycles in a single pass
        /// (avoids N+1 when aggregating the whole fleet).
        /// </summary>
        Task<IReadOnlyDictionary<Guid, FuelConsumptionDTO>> GetConsumptionByMotorcyclesAsync(IEnumerable<Guid> motorcycleIds);
    }
}
