using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Bikontrol.Persistence.Repositories
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly AppDbContext _context;

        public MaintenanceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Maintenance>> GetAllAsync()
        {
            return await _context.DefaultMaintenances
                .Where(mt => mt.IsEnabled)
                .ToListAsync();
        }

        public async Task<IEnumerable<Maintenance>> GetAllForUserAsync(Guid userId)
        {
            return await _context.DefaultMaintenances
                .Where(mt => mt.IsEnabled &&
                    !_context.UserMaintenances
                        .Any(umt => umt.UserId == userId && umt.IsEnabled && umt.BaseTypeId == mt.Id))
                .ToListAsync();
        }

        public async Task<Maintenance?> GetByIdAsync(Guid id)
        {
            return await _context.DefaultMaintenances
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.IsEnabled);
        }
    }
}
