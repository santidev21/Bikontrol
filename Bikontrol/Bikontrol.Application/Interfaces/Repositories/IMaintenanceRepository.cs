using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMaintenanceRepository
    {
        Task<IEnumerable<Maintenance>> GetAllAsync();
        Task<IEnumerable<Maintenance>> GetAllForUserAsync(Guid userId);
        Task<Maintenance?> GetByIdAsync(Guid id);
    }

}
