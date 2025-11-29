using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IUserMaintenanceRepository
    {
        Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId);
        Task<UserMaintenance?> GetByIdAsync(Guid id);

        Task<UserMaintenance> AddAsync(UserMaintenance entity);
        Task UpdateAsync(UserMaintenance entity);

        Task SoftDeleteAsync(Guid id);
    }
}
