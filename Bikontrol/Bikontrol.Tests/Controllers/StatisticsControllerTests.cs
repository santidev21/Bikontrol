using Bikontrol.API.Controllers;
using Bikontrol.Application.DTOs.Statistics;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.Tests.Controllers;

public class StatisticsControllerTests
{
    [Fact]
    public async Task GetSummary_ShouldReturnOkWithSummary()
    {
        var summary = new StatisticsSummaryDTO { TotalMotorcycles = 2, TotalKm = 20000 };
        var service = new FakeStatisticsService { Summary = summary };
        var controller = new StatisticsController(service);

        var result = await controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(summary, ok.Value);
    }

    private sealed class FakeStatisticsService : IStatisticsService
    {
        public StatisticsSummaryDTO Summary { get; set; } = new();
        public Task<StatisticsSummaryDTO> GetSummaryAsync() => Task.FromResult(Summary);
    }
}
