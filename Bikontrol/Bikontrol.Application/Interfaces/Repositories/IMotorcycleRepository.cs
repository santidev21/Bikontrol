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
        Task<Motorcycle> AddAsync(Motorcycle motorcycle);
        Task UpdateAsync(Motorcycle motorcycle);
        Task SoftDeleteAsync(Guid id);

        /// <summary>Persists pending changes. Repositories never save on their own.</summary>
        Task SaveChangesAsync();
    }
}
