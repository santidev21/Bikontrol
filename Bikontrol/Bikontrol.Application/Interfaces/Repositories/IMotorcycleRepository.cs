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

        // The three members below back the photo storage. The defaults are for test
        // fakes; the EF repository overrides them with real queries.

        /// <summary>Sum of stored photo bytes across all of the user's motorcycles (quota).</summary>
        Task<long> GetImageBytesByUserAsync(Guid userId) => Task.FromResult(0L);

        /// <summary>Object keys of the user's photos in object storage, soft-deleted motorcycles included (account deletion).</summary>
        Task<IReadOnlyList<string>> GetImageKeysByUserAsync(Guid userId) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        /// <summary>Motorcycles whose photo is still a data URL in the database, oldest first (storage migration).</summary>
        Task<IReadOnlyList<Motorcycle>> GetPendingImageMigrationAsync(int take) =>
            Task.FromResult<IReadOnlyList<Motorcycle>>(Array.Empty<Motorcycle>());

        Task<Motorcycle> AddAsync(Motorcycle motorcycle);
        Task UpdateAsync(Motorcycle motorcycle);
        Task SoftDeleteAsync(Guid id);

        /// <summary>Persists pending changes. Repositories never save on their own.</summary>
        Task SaveChangesAsync();
    }
}
