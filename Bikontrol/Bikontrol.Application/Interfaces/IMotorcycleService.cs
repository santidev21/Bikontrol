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
        Task<MotorcycleDTO> CreateAsync(CreateMotorcycleDTO dto);
        Task<MotorcycleDTO?> GetByIdAsync(Guid id);
        Task<IList<MotorcycleDTO>> GetByCurrentUserAsync();
        Task UpdateAsync(Guid id, UpdateMotorcycleDTO dto);
        Task SoftDeleteAsync(Guid id);
    }
}
