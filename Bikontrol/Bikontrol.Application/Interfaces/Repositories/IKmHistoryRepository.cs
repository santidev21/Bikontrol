using Bikontrol.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IKmHistoryRepository
    {
        Task AddAsync(MotorcycleKmHistory entity);
        void Remove(MotorcycleKmHistory entity);

        Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId);
        Task<MotorcycleKmHistory?> GetFirstByMotorcycleIdAsync(Guid motorcycleId);

        Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId);

        Task<Dictionary<Guid, int>> GetLatestKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        Task<Dictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        /// <summary>
        /// Takes a row lock on the motorcycle until the surrounding transaction ends, so concurrent
        /// odometer writes for the same bike run one after the other. Must run inside a transaction.
        /// </summary>
        Task LockMotorcycleAsync(Guid motorcycleId);

        Task SaveChangesAsync();
    }
}
