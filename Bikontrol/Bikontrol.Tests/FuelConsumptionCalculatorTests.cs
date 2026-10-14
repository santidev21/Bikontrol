using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;

namespace Bikontrol.Tests;

public class FuelConsumptionCalculatorTests
{
    private static FuelUp Fill(int odometerKm, decimal litres, decimal cost, bool full = true, int dayOffset = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            MotorcycleId = Guid.Empty,
            FueledAt = new DateTime(2026, 1, 1).AddDays(dayOffset),
            OdometerKm = odometerKm,
            Litres = litres,
            TotalCost = cost,
            IsFullTank = full
        };

    [Fact]
    public void Calculate_TwoFullTanks_ComputesOneSegment()
    {
        var motoId = Guid.NewGuid();
        var fuelUps = new[] { Fill(1000, 5m, 10m), Fill(1200, 4m, 8m, dayOffset: 5) };

        var result = FuelConsumptionCalculator.Calculate(motoId, fuelUps, 0m);

        var point = Assert.Single(result.Points);
        Assert.Equal(200, point.DistanceKm);
        Assert.Equal(4m, point.Litres);
        Assert.Equal(50m, point.KmPerLitre); // 200 km / 4 L
        Assert.Equal(50m, result.AverageKmPerLitre);
        Assert.Equal(200, result.TotalDistanceKm);
    }

    [Fact]
    public void Calculate_PartialFill_IsRolledIntoNextFullTank()
    {
        var fuelUps = new[]
        {
            Fill(1000, 5m, 10m),                       // baseline
            Fill(1100, 2m, 4m, full: false),           // partial
            Fill(1300, 3m, 6m, dayOffset: 10)          // full -> segment over 300 km / 5 L
        };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 0m);

        var point = Assert.Single(result.Points);
        Assert.Equal(300, point.DistanceKm);
        Assert.Equal(5m, point.Litres);
        Assert.Equal(60m, point.KmPerLitre);
    }

    [Fact]
    public void Calculate_FirstFullTank_OpensNoSegment()
    {
        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), new[] { Fill(1000, 5m, 10m) }, 0m);

        Assert.Empty(result.Points);
        Assert.Null(result.AverageKmPerLitre);
        Assert.Equal(0, result.TotalDistanceKm);
    }

    [Fact]
    public void Calculate_NonIncreasingOdometer_IsSkipped()
    {
        var fuelUps = new[]
        {
            Fill(1000, 5m, 10m),
            Fill(1000, 5m, 10m, dayOffset: 1), // same odometer: no distance
            Fill(1200, 4m, 8m, dayOffset: 2)
        };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 0m);

        var point = Assert.Single(result.Points);
        Assert.Equal(200, point.DistanceKm);
    }

    [Fact]
    public void Calculate_AddsMaintenanceCostToTotalCostOfOwnership()
    {
        var fuelUps = new[] { Fill(1000, 5m, 20m), Fill(1200, 4m, 10m, dayOffset: 5) };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 70m);

        Assert.Equal(30m, result.TotalFuelCost);
        Assert.Equal(70m, result.TotalMaintenanceCost);
        Assert.Equal(100m, result.TotalCostOfOwnership);
        Assert.Equal(0.15m, result.FuelCostPerKm); // 30 / 200 km
        Assert.Equal(0.5m, result.CostPerKm);      // 100 / 200 km
    }

    [Fact]
    public void Calculate_NoDistance_ReturnsNullCostPerKm()
    {
        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), new[] { Fill(1000, 5m, 10m) }, 0m);

        Assert.Null(result.FuelCostPerKm);
        Assert.Null(result.CostPerKm);
    }

    [Fact]
    public void Calculate_EmptyLog_ReturnsZeros()
    {
        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), Array.Empty<FuelUp>(), 0m);

        Assert.Empty(result.Points);
        Assert.Equal(0m, result.TotalFuelCost);
        Assert.Equal(0, result.TotalDistanceKm);
        Assert.False(result.IsConsumptionDrop);
    }

    [Fact]
    public void Calculate_SharpDrop_FlagsPossibleMechanicalProblem()
    {
        var fuelUps = new[]
        {
            Fill(0, 10m, 20m),                 // baseline
            Fill(400, 10m, 20m, dayOffset: 1), // 40 km/l
            Fill(800, 10m, 20m, dayOffset: 2), // 40 km/l
            Fill(1200, 10m, 20m, dayOffset: 3),// 40 km/l
            Fill(1500, 12m, 24m, dayOffset: 4) // 25 km/l -> drop
        };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 0m);

        Assert.True(result.IsConsumptionDrop);
        Assert.Equal(25m, result.LatestKmPerLitre);
        Assert.Equal(40m, result.BaselineKmPerLitre);
    }

    [Fact]
    public void Calculate_StableConsumption_DoesNotFlagDrop()
    {
        var fuelUps = new[]
        {
            Fill(0, 10m, 20m),
            Fill(400, 10m, 20m, dayOffset: 1),
            Fill(800, 10m, 20m, dayOffset: 2),
            Fill(1200, 10m, 20m, dayOffset: 3),
            Fill(1590, 10m, 20m, dayOffset: 4) // 39 km/l -> stable
        };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 0m);

        Assert.False(result.IsConsumptionDrop);
    }

    [Fact]
    public void Calculate_TooFewSegments_DoesNotFlagDrop()
    {
        var fuelUps = new[]
        {
            Fill(0, 10m, 20m),
            Fill(400, 10m, 20m, dayOffset: 1),
            Fill(500, 10m, 20m, dayOffset: 2) // only 1 segment
        };

        var result = FuelConsumptionCalculator.Calculate(Guid.NewGuid(), fuelUps, 0m);

        Assert.False(result.IsConsumptionDrop);
        Assert.Null(result.LatestKmPerLitre);
    }
}
