using Bikontrol.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Persistence
{
    public class TransactionManager : ITransactionManager
    {
        private readonly AppDbContext _context;

        public TransactionManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await ExecuteInTransactionAsync<object?>(async () =>
            {
                await action();
                return null;
            }, cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            // Si ya hay una transacción en curso (llamadas anidadas entre servicios),
            // la acción se une a ella en lugar de abrir una nueva.
            if (_context.Database.CurrentTransaction is not null)
            {
                return await action();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
