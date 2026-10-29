using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Application.Services
{
    public class KmHistoryService : IKmHistoryService
    {
        private readonly IKmHistoryRepository _repository;
        private readonly ITransactionManager _transactions;

        public KmHistoryService(IKmHistoryRepository repository, ITransactionManager transactions)
        {
            _repository = repository;
            _transactions = transactions;
        }

        public async Task AddKmAsync(Guid motorcycleId, int km)
        {
            // The motorcycle row lock serialises concurrent writers: the second one re-reads
            // the odometer after the first has committed instead of racing on a stale value.
            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                await _repository.LockMotorcycleAsync(motorcycleId);

                var last = await _repository.GetLastByMotorcycleIdAsync(motorcycleId);

                if (last != null && km < last.Km)
                    throw new ValidationException("No puedes registrar un kilometraje menor al actual.", "km_below_current");

                if (last != null && km == last.Km)
                    return;

                await _repository.AddAsync(new MotorcycleKmHistory
                {
                    MotorcycleId = motorcycleId,
                    Km = km
                });
            });
        }

        public async Task<int> GetCurrentKmAsync(Guid motorcycleId)
        {
            var last = await _repository.GetLastByMotorcycleIdAsync(motorcycleId);
            return last?.Km ?? 0;
        }

        public async Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId)
        {
            var first = await _repository.GetFirstByMotorcycleIdAsync(motorcycleId);
            return first?.RecordedAt;
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
            => await _repository.GetLatestKmByMotorcycleIdsAsync(motorcycleIds);

        public async Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds)
            => await _repository.GetInitialRecordedAtByMotorcycleIdsAsync(motorcycleIds);

        public async Task RollbackLastKmAsync(Guid motorcycleId, int newKm)
        {
            var history = await _repository.GetByMotorcycleIdAsync(motorcycleId);
            if (history.Count <= 1)
                throw new ValidationException("No puedes eliminar el registro inicial de kilometraje.", "km_initial_protected");

            var last = history[0];
            var previous = history[1];

            if (newKm > last.Km)
                throw new ValidationException("El nuevo kilometraje no puede ser mayor al registro eliminado.", "km_above_rollback");

            if (newKm < previous.Km)
                throw new ValidationException("El nuevo kilometraje no puede ser menor al penultimo registro.", "km_below_penultimate");

            // Eliminar + re-agregar en una sola transacción.
            await _transactions.ExecuteInTransactionAsync(async () =>
            {
                _repository.Remove(last);
                await _repository.SaveChangesAsync();

                if (newKm == last.Km)
                    return;

                if (newKm == previous.Km)
                    return;

                await AddKmAsync(motorcycleId, newKm);
            });
        }
    }
}
