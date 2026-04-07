using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                throw new Exception("Km cannot decrease");

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
    }
}
