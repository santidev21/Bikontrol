namespace Bikontrol.Domain.Entities
{
    public class MotorcycleMaintenanceRecord
    {
        public Guid Id { get; set; }
        public Guid MotorcycleId { get; set; }
        public Guid UserMaintenanceId { get; set; }
        public DateTime PerformedAt { get; set; }
        public int? PerformedKm { get; set; }

        /// <summary>
        /// Cost of the maintenance in the user's currency (optional). Used for
        /// the cost statistics (spend per motorcycle/year, cost per km).
        /// </summary>
        public decimal? Cost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Motorcycle Motorcycle { get; set; } = null!;
        public UserMaintenance UserMaintenance { get; set; } = null!;
        public ICollection<MaintenanceRecordAttachment> Attachments { get; set; } = new List<MaintenanceRecordAttachment>();
    }
}
