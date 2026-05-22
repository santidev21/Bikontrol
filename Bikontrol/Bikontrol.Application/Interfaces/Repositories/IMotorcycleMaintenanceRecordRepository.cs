using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMotorcycleMaintenanceRecordRepository
    {
        Task<MotorcycleMaintenanceRecord> AddAsync(MotorcycleMaintenanceRecord entity);
        Task<IEnumerable<MotorcycleMaintenanceRecord>> GetByMotorcycleIdAsync(Guid motorcycleId);
        Task<MotorcycleMaintenanceRecord?> GetLastByUserMaintenanceIdAsync(Guid userMaintenanceId);
    }
}
