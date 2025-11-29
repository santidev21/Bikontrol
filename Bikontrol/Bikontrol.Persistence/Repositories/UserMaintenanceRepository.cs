using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class UserMaintenanceRepository : IUserMaintenanceRepository
    {
        private readonly AppDbContext _context;

        public UserMaintenanceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserMaintenance>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserMaintenances
                .Where(umt => umt.UserId == userId && umt.IsEnabled)
                .ToListAsync();
        }

        public async Task<UserMaintenance?> GetByIdAsync(Guid id)
        {
            return await _context.UserMaintenances
                .FirstOrDefaultAsync(umt => umt.Id == id && umt.IsEnabled);
        }

        public async Task<UserMaintenance> AddAsync(UserMaintenance entity)
        {
            _context.UserMaintenances.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(UserMaintenance entity)
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
