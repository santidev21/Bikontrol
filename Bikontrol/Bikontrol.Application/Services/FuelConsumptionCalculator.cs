using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Derives consumption and cost metrics from a motorcycle's fuel-ups. Pure and
    /// dependency-free so it is cheap to unit-test and mutation-test.
    ///
    /// Consumption uses the full-to-full method: a segment runs from one full tank
    /// to the next and its litres include any partial fills in between, so
    /// recording the odd half-tank does not skew the average. A sharp drop in
    /// consumption doubles as an early sign of a mechanical problem.
    /// </summary>
    public static class FuelConsumptionCalculator
    {
        /// <summary>How far (percent) below the recent average counts as a drop.</summary>
        public const decimal DropThresholdPercent = 20m;

        /// <summary>How many previous segments form the baseline.</summary>
        public const int BaselineWindow = 5;

        /// <summary>Segments needed before a drop can be flagged (latest + two baseline).</summary>
        public const int MinimumSegmentsForDrop = 3;

        public static FuelConsumptionDTO Calculate(
            Guid motorcycleId,
            IEnumerable<FuelUp>? fuelUps,
            decimal maintenanceCost)
        {
            var ordered = (fuelUps ?? Enumerable.Empty<FuelUp>())
                .OrderBy(f => f.OdometerKm)
                .ThenBy(f => f.FueledAt)
                .ThenBy(f => f.CreatedAt)
                .ToList();

            var result = new FuelConsumptionDTO
            {
                MotorcycleId = motorcycleId,
                TotalFuelCost = ordered.Sum(f => f.TotalCost),
                TotalLitres = ordered.Sum(f => f.Litres),
                TotalMaintenanceCost = maintenanceCost
            };
            result.TotalCostOfOwnership = result.TotalFuelCost + maintenanceCost;

            if (ordered.Count > 0)
            {
                var min = ordered[0].OdometerKm;
                var max = ordered[^1].OdometerKm;
                result.TotalDistanceKm = Math.Max(0, max - min);
            }

            int? lastFullOdometer = null;
            decimal segmentLitres = 0m;
            decimal segmentCost = 0m;

            foreach (var fuelUp in ordered)
            {
                segmentLitres += fuelUp.Litres;
                segmentCost += fuelUp.TotalCost;

                if (!fuelUp.IsFullTank)
                    continue;

                if (lastFullOdometer is null)
                {
                    // The first full tank defines the baseline; its litres fuelled
                    // the distance before the log starts, so it opens no segment.
                    lastFullOdometer = fuelUp.OdometerKm;
                    segmentLitres = 0m;
                    segmentCost = 0m;
                    continue;
                }

                var distance = fuelUp.OdometerKm - lastFullOdometer.Value;
                if (distance <= 0 || segmentLitres <= 0)
                {
                    // Non-increasing odometer or no fuel recorded: not a segment.
                    lastFullOdometer = fuelUp.OdometerKm;
                    segmentLitres = 0m;
                    segmentCost = 0m;
                    continue;
                }

                result.Points.Add(new FuelConsumptionPointDTO
                {
                    FueledAt = fuelUp.FueledAt,
                    OdometerKm = fuelUp.OdometerKm,
                    DistanceKm = distance,
                    Litres = segmentLitres,
                    Cost = segmentCost,
                    KmPerLitre = Math.Round(distance / segmentLitres, 2)
                });

                lastFullOdometer = fuelUp.OdometerKm;
                segmentLitres = 0m;
                segmentCost = 0m;
            }

            // Every recorded point has litres > 0 (and distance > 0), so the total
            // litres alone tells us whether an average can be computed.
            var segmentDistance = result.Points.Sum(p => p.DistanceKm);
            var segmentLitresTotal = result.Points.Sum(p => p.Litres);
            if (segmentLitresTotal > 0)
                result.AverageKmPerLitre = Math.Round(segmentDistance / segmentLitresTotal, 2);

            result.FuelCostPerKm = result.TotalDistanceKm > 0
                ? Math.Round(result.TotalFuelCost / result.TotalDistanceKm, 4)
                : null;
            result.CostPerKm = result.TotalDistanceKm > 0
                ? Math.Round(result.TotalCostOfOwnership / result.TotalDistanceKm, 4)
                : null;

            ApplyDropDetection(result);
            return result;
        }

        private static void ApplyDropDetection(FuelConsumptionDTO result)
        {
            var consumptions = result.Points
                .Where(p => p.KmPerLitre is > 0)
                .Select(p => p.KmPerLitre!.Value)
                .ToList();

            if (consumptions.Count < MinimumSegmentsForDrop)
                return;

            var latest = consumptions[^1];
            var baseline = consumptions
                .Take(consumptions.Count - 1)
                .TakeLast(BaselineWindow)
                .Average();

            result.LatestKmPerLitre = latest;
            result.BaselineKmPerLitre = Math.Round(baseline, 2);

            if (baseline > 0 && latest < baseline * (1 - DropThresholdPercent / 100m))
                result.IsConsumptionDrop = true;
        }
    }
}
