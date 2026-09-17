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
                .FirstOrDefaultAsync();
        }

        public async Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderBy(x => x.RecordedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, int>> GetLatestKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, int>();

            var rows = await _context.MotorcycleKmHistories
                .Where(x => ids.Contains(x.MotorcycleId))
                .Select(x => new { x.MotorcycleId, x.Km, x.RecordedAt, x.Id })
                .ToListAsync();

            return rows
                .GroupBy(x => x.MotorcycleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).First().Km);
        }

        public async Task<Dictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
        {
            var ids = motorcycleIds?.Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, DateTime?>();

            var rows = await _context.MotorcycleKmHistories
                .Where(x => ids.Contains(x.MotorcycleId))
                .Select(x => new { x.MotorcycleId, x.RecordedAt })
                .ToListAsync();

            return rows
                .GroupBy(x => x.MotorcycleId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Min(x => x.RecordedAt));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
