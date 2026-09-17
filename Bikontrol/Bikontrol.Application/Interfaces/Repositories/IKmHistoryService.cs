using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IKmHistoryService
    {
        Task AddKmAsync(Guid motorcycleId, int km);

        Task<int> GetCurrentKmAsync(Guid motorcycleId);
        Task<DateTime?> GetInitialRecordedAtAsync(Guid motorcycleId);

        /// <summary>Km actual de varias motos en una sola consulta (evita N+1).</summary>
        Task<IReadOnlyDictionary<Guid, int>> GetCurrentKmByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        /// <summary>Primer registro (fecha) de varias motos en una sola consulta.</summary>
        Task<IReadOnlyDictionary<Guid, DateTime?>> GetInitialRecordedAtByMotorcycleIdsAsync(IEnumerable<Guid> motorcycleIds);

        Task RollbackLastKmAsync(Guid motorcycleId, int newKm);
    }
}
