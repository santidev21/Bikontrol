using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMaintenanceTypeRepository
    {
        Task<IEnumerable<MaintenanceType>> GetAllAsync();
        Task<MaintenanceType?> GetByIdAsync(Guid id);
    }

}
