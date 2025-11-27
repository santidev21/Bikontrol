using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class UserMaintenanceTypeRepository : IUserMaintenanceTypeRepository
    {
        private readonly AppDbContext _context;

        public UserMaintenanceTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserMaintenanceType>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserMaintenances
                .Where(umt => umt.UserId == userId && umt.IsEnabled)
                .ToListAsync();
        }

        public async Task<UserMaintenanceType?> GetByIdAsync(Guid id)
        {
            return await _context.UserMaintenances
                .FirstOrDefaultAsync(umt => umt.Id == id && umt.IsEnabled);
        }

        public async Task<UserMaintenanceType> AddAsync(UserMaintenanceType entity)
        {
            _context.UserMaintenances.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(UserMaintenanceType entity)
        {
            _context.UserMaintenances.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var entity = await _context.UserMaintenances
                .FirstOrDefaultAsync(umt => umt.Id == id);

            if (entity is not null)
            {
                entity.IsEnabled = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
