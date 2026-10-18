using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    public class FuelUpRepository : IFuelUpRepository
    {
        private readonly AppDbContext _context;

        public FuelUpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FuelUp> AddAsync(FuelUp entity)
        {
            await _context.FuelUps.AddAsync(entity);
            return entity;
        }

        public async Task<FuelUp?> GetByIdAsync(Guid id)
        {
            return await _context.FuelUps.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.FuelUps
                .AsNoTracking()
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.FueledAt)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<FuelUp>> GetByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new List<FuelUp>();

            return await _context.FuelUps
                .AsNoTracking()
                .Where(x => ids.Contains(x.MotorcycleId))
                .OrderBy(x => x.MotorcycleId)
                .ThenBy(x => x.OdometerKm)
                .ToListAsync();
        }

        public Task UpdateAsync(FuelUp entity)
        {
            _context.FuelUps.Update(entity);
            return Task.CompletedTask;
        }

        public void Remove(FuelUp entity)
        {
            _context.FuelUps.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
