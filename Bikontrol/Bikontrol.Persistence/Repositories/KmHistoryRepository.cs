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
    public class KmHistoryRepository : IKmHistoryRepository
    {
        private readonly AppDbContext _context;
        public KmHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task LockMotorcycleAsync(Guid motorcycleId)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Motorcycles\" WHERE \"Id\" = {motorcycleId} FOR UPDATE");
        }

        public async Task AddAsync(MotorcycleKmHistory entity)
        {
            await _context.MotorcycleKmHistories.AddAsync(entity);
        }

        public void Remove(MotorcycleKmHistory entity)
        {
            _context.MotorcycleKmHistories.Remove(entity);
        }

        public async Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .AsNoTracking()
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderBy(x => x.RecordedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .AsNoTracking()
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, int>> GetLatestKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, int>();

            // One row per motorcycle computed in the database instead of loading
            // the whole history of every motorcycle and grouping in memory.
            return await _context.MotorcycleKmHistories
                .Where(x => ids.Contains(x.MotorcycleId))
                .GroupBy(x => x.MotorcycleId)
                .Select(g => new
                {
                    MotorcycleId = g.Key,
                    Km = g.OrderByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).Select(x => x.Km).First()
                })
                .ToDictionaryAsync(x => x.MotorcycleId, x => x.Km);
        }

        public async Task<Dictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, DateTime?>();

            return await _context.MotorcycleKmHistories
                .Where(x => ids.Contains(x.MotorcycleId))
                .GroupBy(x => x.MotorcycleId)
                .Select(g => new { MotorcycleId = g.Key, Initial = (DateTime?)g.Min(x => x.RecordedAt) })
                .ToDictionaryAsync(x => x.MotorcycleId, x => x.Initial);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
