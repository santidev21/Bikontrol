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
            if (entity is not null)
            {
                entity.IsEnabled = false;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
