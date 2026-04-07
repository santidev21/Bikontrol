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

        Task<MotorcycleKmHistory?> GetLastByMotorcycleIdAsync(Guid motorcycleId);

        Task<List<MotorcycleKmHistory>> GetByMotorcycleIdAsync(Guid motorcycleId);

        Task SaveChangesAsync();
    }
}
