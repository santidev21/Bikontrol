using Bikontrol.Application.DTOs.Motorcycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    public interface IMotorcycleService
    {
        Task<MotorcycleDTO> CreateAsync(SaveMotorcycleDTO dto);
        Task<MotorcycleDTO?> GetByIdAsync(Guid id);
        Task<IList<MotorcycleDTO>> GetByCurrentUserAsync();
        Task<int> GetCurrentKmAsync(Guid id);
        Task AddKmHistoryAsync(Guid id, int km);
        Task RollbackLastKmAsync(Guid id, int newKm);
        Task UpdateAsync(Guid id, SaveMotorcycleDTO dto);
        Task SoftDeleteAsync(Guid id);

        /// <summary>
        /// Opens the photo behind a signed media URL. Anonymous by design (an
        /// <c>&lt;img&gt;</c> cannot send a bearer token): the signature is the
        /// credential, and a bad, expired or unknown one is simply not found.
        /// </summary>
        Task<StoredObject> OpenImageAsync(Guid id, long expiresUnix, string? signature, CancellationToken cancellationToken = default);
    }
}
