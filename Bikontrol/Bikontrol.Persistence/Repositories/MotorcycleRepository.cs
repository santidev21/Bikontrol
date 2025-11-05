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
            _context.Motorcycles.Add(motorcycle);
            await _context.SaveChangesAsync();
            return motorcycle;
        }

        public async Task UpdateAsync(Motorcycle motorcycle)
        {
            _context.Motorcycles.Update(motorcycle);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            var entity = await _context.Motorcycles.FirstOrDefaultAsync(m => m.Id == id);
            if (entity is not null)
            {
                entity.IsEnabled = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
