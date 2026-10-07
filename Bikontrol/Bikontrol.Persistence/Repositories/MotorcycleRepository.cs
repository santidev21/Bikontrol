using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Repositories
{
    public class MotorcycleRepository : IMotorcycleRepository
    {
        private readonly AppDbContext _context;

        public MotorcycleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Motorcycle?> GetByIdAsync(Guid id)
        {
            return await _context.Motorcycles.FirstOrDefaultAsync(m => m.Id == id && m.IsEnabled);
        }

        public async Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Motorcycles
                .Where(m => m.UserId == userId && m.IsEnabled)
                .ToListAsync();
        }

        public async Task<Motorcycle> AddAsync(Motorcycle motorcycle)
        {
            await _context.Motorcycles.AddAsync(motorcycle);
            return motorcycle;
        }

        public Task UpdateAsync(Motorcycle motorcycle)
        {
            _context.Motorcycles.Update(motorcycle);
            return Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var entity = await _context.Motorcycles.FirstOrDefaultAsync(m => m.Id == id);
            if (entity is null)
                return;

            // Soft-deleting a motorcycle would leave its maintenance records
            // pointing at a disabled parent, which the integrity audit rejects
            // (checks 5 & 8). Remove the now-unreachable records (their
            // attachments cascade via the FK) and disable the motorcycle's
            // maintenances, on the same connection so the caller's transaction
            // keeps the whole operation atomic.
            await _context.MotorcycleMaintenanceRecords
                .Where(r => r.MotorcycleId == id)
                .ExecuteDeleteAsync();
            await _context.UserMaintenances
                .Where(um => um.MotorcycleId == id && um.IsEnabled)
                .ExecuteUpdateAsync(s => s.SetProperty(um => um.IsEnabled, false));

            entity.IsEnabled = false;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
