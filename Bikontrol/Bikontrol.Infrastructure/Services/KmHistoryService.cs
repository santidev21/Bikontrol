using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
{
    public class KmHistoryService : IKmHistoryService
    {
        private readonly IKmHistoryRepository _repository;

        public KmHistoryService(IKmHistoryRepository repository)
        {
            _repository = repository;
        }

        public async Task AddKmAsync(Guid motorcycleId, int km)
        {
            var last = await _repository.GetLastByMotorcycleIdAsync(motorcycleId);

            if (last != null && km < last.Km)
                throw new ValidationException("No puedes registrar un kilometraje menor al actual.");

            if (last != null && km == last.Km)
                return;

            var entity = new MotorcycleKmHistory
            {
                MotorcycleId = motorcycleId,
                Km = km
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
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

        public async Task RollbackLastKmAsync(Guid motorcycleId, int newKm)
        {
            var history = await _repository.GetByMotorcycleIdAsync(motorcycleId);
            if (history.Count <= 1)
                throw new ValidationException("No puedes eliminar el registro inicial de kilometraje.");

            var last = history[0];
            var previous = history[1];

            if (newKm > last.Km)
                throw new ValidationException("El nuevo kilometraje no puede ser mayor al registro eliminado.");

            if (newKm < previous.Km)
                throw new ValidationException("El nuevo kilometraje no puede ser menor al penultimo registro.");

            _repository.Remove(last);
            await _repository.SaveChangesAsync();

            if (newKm == last.Km)
                return;

            if (newKm == previous.Km)
                return;

            await AddKmAsync(motorcycleId, newKm);
        }
    }
}
