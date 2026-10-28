using Bikontrol.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace Bikontrol.Persistence
{
    /// <summary>
    /// <see cref="IDistributedLock"/> on a Postgres session advisory lock. It
    /// holds a dedicated connection for as long as the lock is held, so the lock
    /// is released if the process dies (the server drops the session).
    /// </summary>
    public class PostgresAdvisoryLock : IDistributedLock
    {
        private readonly AppDbContext _context;

        public PostgresAdvisoryLock(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken = default)
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("No database connection string is configured.");

            var key = ToKey(name);
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
                command.Parameters.AddWithValue("key", key);
                var acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
                if (!acquired)
                {
                    await connection.DisposeAsync();
                    return null;
                }
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }

            return new Handle(connection, key);
        }

        /// <summary>Stable 64-bit key derived from the lock name.</summary>
        private static long ToKey(string name) =>
            BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(name)), 0);

        private sealed class Handle : IAsyncDisposable
        {
            private readonly NpgsqlConnection _connection;
            private readonly long _key;

            public Handle(NpgsqlConnection connection, long key)
            {
                _connection = connection;
                _key = key;
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", _connection);
                    command.Parameters.AddWithValue("key", _key);
                    await command.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Closing the connection below releases the lock anyway.
                }
                finally
                {
                    await _connection.DisposeAsync();
                }
            }
        }
    }
}
