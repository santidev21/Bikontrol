using Bikontrol.Application.DTOs.Statistics;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Agregados de solo lectura sobre la flota del usuario actual.
    /// No escribe ni migra nada: es seguro contra los datos existentes.
    /// </summary>
    public interface IStatisticsService
    {
        Task<StatisticsSummaryDTO> GetSummaryAsync();
    }
}
