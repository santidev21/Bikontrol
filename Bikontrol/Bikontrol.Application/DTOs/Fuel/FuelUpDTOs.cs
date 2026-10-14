using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Fuel
{
    /// <summary>A logged fuel-up with its derived price per litre.</summary>
    public class FuelUpDTO
    {
        public Guid Id { get; set; }
        public Guid MotorcycleId { get; set; }
        public DateTime FueledAt { get; set; }
        public int OdometerKm { get; set; }
        public decimal Litres { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PricePerLitre { get; set; }
        public bool IsFullTank { get; set; }
        public string? Station { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Create/update payload for a fuel-up.</summary>
    public class SaveFuelUpRequest
    {
        [Required(ErrorMessage = "La motocicleta es obligatoria.")]
        public Guid MotorcycleId { get; set; }

        [Required(ErrorMessage = "La fecha de la carga es obligatoria.")]
        public DateTime FueledAt { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 1,000,000 km.")]
        public int OdometerKm { get; set; }

        [Range(0.01, 10_000, ErrorMessage = "Los litros deben estar entre 0.01 y 10,000.")]
        public decimal Litres { get; set; }

        [Range(0, 100_000_000, ErrorMessage = "El costo debe estar entre 0 y 100,000,000.")]
        public decimal TotalCost { get; set; }

        /// <summary>Defaults to true; only full tanks delimit a consumption segment.</summary>
        public bool IsFullTank { get; set; } = true;

        [MaxLength(120, ErrorMessage = "La estación no puede superar los 120 caracteres.")]
        public string? Station { get; set; }

        [MaxLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
        public string? Notes { get; set; }
    }

    /// <summary>One full-to-full consumption segment.</summary>
    public class FuelConsumptionPointDTO
    {
        public DateTime FueledAt { get; set; }
        public int OdometerKm { get; set; }

        /// <summary>Distance since the previous full tank, in km.</summary>
        public int DistanceKm { get; set; }

        /// <summary>Fuel consumed over that distance, in litres.</summary>
        public decimal Litres { get; set; }

        /// <summary>Spend over that distance, in the user's currency.</summary>
        public decimal Cost { get; set; }

        /// <summary>km per litre for this segment; null when it cannot be computed.</summary>
        public decimal? KmPerLitre { get; set; }
    }

    /// <summary>
    /// Consumption and cost-of-ownership metrics derived from a motorcycle's
    /// fuel-ups plus its recorded maintenance spend.
    /// </summary>
    public class FuelConsumptionDTO
    {
        public Guid MotorcycleId { get; set; }

        /// <summary>Fleet/segment km per litre (full-to-full, weighted); null without data.</summary>
        public decimal? AverageKmPerLitre { get; set; }

        /// <summary>Fuel cost per km over the logged distance; null when km is 0.</summary>
        public decimal? FuelCostPerKm { get; set; }

        /// <summary>Total cost of ownership (fuel + maintenance) per km; null when km is 0.</summary>
        public decimal? CostPerKm { get; set; }

        public decimal TotalFuelCost { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public decimal TotalCostOfOwnership { get; set; }
        public decimal TotalLitres { get; set; }

        /// <summary>Distance between the first and last logged odometer readings.</summary>
        public int TotalDistanceKm { get; set; }

        /// <summary>Consumption of the most recent segment; null without enough data.</summary>
        public decimal? LatestKmPerLitre { get; set; }

        /// <summary>Average of the recent segments used as the drop baseline.</summary>
        public decimal? BaselineKmPerLitre { get; set; }

        /// <summary>True when the latest consumption dropped sharply (possible mechanical issue).</summary>
        public bool IsConsumptionDrop { get; set; }

        public List<FuelConsumptionPointDTO> Points { get; set; } = new();
    }
}
