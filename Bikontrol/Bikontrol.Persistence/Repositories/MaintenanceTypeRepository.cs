using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class MaintenanceTypeRepository : IMaintenanceTypeRepository
    {
        private readonly AppDbContext _context;

        public MaintenanceTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MaintenanceType>> GetAllAsync()
        {
            return await _context.DefaultMaintenances
                .Where(mt => mt.IsEnabled)
                .ToListAsync();
        }

        public async Task<MaintenanceType?> GetByIdAsync(Guid id)
        {
            return await _context.DefaultMaintenances
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.IsEnabled);
        }
    }
}
