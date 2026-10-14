using System;

namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// A fuel-up (refuelling): how much fuel, at what price and at which odometer
    /// reading. Fuel-ups are the raw data behind consumption (km/l), fuel cost per
    /// km and total cost of ownership. The odometer also feeds the motorcycle km
    /// history, so a fuel-up is a natural checkpoint for the maintenance countdown.
    /// </summary>
    public class FuelUp
    {
        public Guid Id { get; set; }

        public Guid MotorcycleId { get; set; }

        public DateTime FueledAt { get; set; }

        /// <summary>Odometer reading at the pump, in km.</summary>
        public int OdometerKm { get; set; }

        /// <summary>Fuel added, in litres. Must be greater than zero.</summary>
        public decimal Litres { get; set; }

        /// <summary>Amount paid, in the user's currency.</summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// True when the tank was filled to the top. Only full tanks delimit a
        /// consumption segment, so partial fills are added to the next one.
        /// </summary>
        public bool IsFullTank { get; set; } = true;

        public string? Station { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Motorcycle Motorcycle { get; set; } = null!;

        /// <summary>Derived price per litre. Not persisted.</summary>
        public decimal PricePerLitre => Litres > 0 ? Math.Round(TotalCost / Litres, 3) : 0m;
    }
}
