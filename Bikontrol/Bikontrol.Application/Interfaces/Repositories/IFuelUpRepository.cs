using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IFuelUpRepository
    {
        Task<FuelUp> AddAsync(FuelUp entity);

        Task<FuelUp?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdAsync(Guid motorcycleId);

        /// <summary>Cargas de varias motos en una sola consulta (evita N+1).</summary>
        Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        Task UpdateAsync(FuelUp entity);

        void Remove(FuelUp entity);

        Task SaveChangesAsync();
    }
}
