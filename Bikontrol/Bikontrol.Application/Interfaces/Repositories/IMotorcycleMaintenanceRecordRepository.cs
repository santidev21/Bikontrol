using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMotorcycleMaintenanceRecordRepository
    {
        Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity);
        Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId);

        /// <summary>Registros de varias motos en una sola consulta (evita N+1).</summary>
        Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId);

        /// <summary>
        /// Último registro por mantenimiento en una sola consulta (evita N+1).
        /// </summary>
        Task<Dictionary<Guid, MotorcycleMaintenanceRecord>> GetLastByUserMaintenanceIdsAsync(IEnumerable<Guid> userMaintenanceIds);
    }
}
