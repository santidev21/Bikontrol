using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IUserMaintenanceRepository
    {
        Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserMaintenance>> GetByUserIdAndMotorcycleIdAsync(Guid userId, Guid motorcycleId);
        Task<UserMaintenance?> GetByIdAsync(Guid id);

        Task<UserMaintenance> AddAsync(UserMaintenance entity);
        Task UpdateAsync(UserMaintenance entity);

        Task SoftDeleteAsync(Guid id);
        Task<UserMaintenance?> GetByBaseIdAsync(Guid userId, Guid motorcycleId, Guid baseId);
    }
}
