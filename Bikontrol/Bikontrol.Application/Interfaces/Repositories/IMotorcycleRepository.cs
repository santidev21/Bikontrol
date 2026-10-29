using Bikontrol.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IMotorcycleRepository
    {
        Task<Motorcycle?> GetByIdAsync(Guid id);
        Task<IEnumerable<Motorcycle>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Id → display name of the user's enabled motorcycles. Lets callers that
        /// only need labels skip the (up to ~1 MB) <c>Image</c> data URL that
        /// <see cref="GetByUserIdAsync"/> loads. The default goes through the full
        /// query; the EF repository overrides it with a narrow projection.
        /// </summary>
        async Task<IReadOnlyDictionary<Guid, string>> GetNamesByUserIdAsync(Guid userId) =>
            (await GetByUserIdAsync(userId)).ToDictionary(m => m.Id, m => m.Name);

        Task<Motorcycle> AddAsync(Motorcycle motorcycle);
        Task UpdateAsync(Motorcycle motorcycle);
        Task SoftDeleteAsync(Guid id);

        /// <summary>Persists pending changes. Repositories never save on their own.</summary>
        Task SaveChangesAsync();
    }
}
