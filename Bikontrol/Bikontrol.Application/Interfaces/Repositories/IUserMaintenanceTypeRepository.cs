using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IUserMaintenanceTypeRepository
    {
        Task<IEnumerable<UserMaintenanceType>> GetByUserIdAsync(Guid userId);
        Task<UserMaintenanceType?> GetByIdAsync(Guid id);

        Task<UserMaintenanceType> AddAsync(UserMaintenanceType entity);
        Task UpdateAsync(UserMaintenanceType entity);

        Task SoftDeleteAsync(Guid id);
    }
}
