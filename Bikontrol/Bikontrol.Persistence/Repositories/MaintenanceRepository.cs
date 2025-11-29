using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        public async Task<Maintenance?> GetByIdAsync(Guid id)
        {
            return await _context.DefaultMaintenances
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.IsEnabled);
        }
    }
}
