using Bikontrol.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bikontrol.Persistence.Repositories
{
    /// <summary>
    /// Account-level bulk operations. Executes deletes immediately (not via
    /// SaveChanges) so the caller must wrap it in a transaction; the deleted rows
    /// are intentionally not audited (the payloads would bloat the trail with the
    /// very PII being erased).
    /// </summary>
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task PurgeUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var motorcycleIds = await _context.Motorcycles
                .Where(m => m.UserId == userId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var recordIds = await _context.MotorcycleMaintenanceRecords
                .Where(r => motorcycleIds.Contains(r.MotorcycleId))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            // Children first, then the motorcycles they hang off. The database
            // would cascade, but deleting explicitly keeps the intent obvious and
            // survives a future change to restrict a FK.
            await _context.MaintenanceRecordAttachments
                .Where(a => recordIds.Contains(a.MotorcycleMaintenanceRecordId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.MotorcycleMaintenanceRecords
                .Where(r => recordIds.Contains(r.Id))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.UserMaintenances
                .Where(m => m.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _context.MotorcycleKmHistories
                .Where(k => motorcycleIds.Contains(k.MotorcycleId))
                .ExecuteDeleteAsync(cancellationToken);
            await _context.Motorcycles
                .Where(m => m.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.ReminderLogs
                .Where(r => r.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _context.PushSubscriptions
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _context.RefreshTokens
                .Where(t => t.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
