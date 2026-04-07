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
    }
}
