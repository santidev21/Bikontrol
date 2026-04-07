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

        public async Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId)
        {
            return await _context.MotorcycleKmHistories
                .Where(x => x.MotorcycleId == motorcycleId)
                .OrderByDescending(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
