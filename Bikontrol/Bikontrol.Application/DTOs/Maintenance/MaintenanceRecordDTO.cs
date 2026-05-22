namespace Bikontrol.Application.DTOs.Maintenance
{
    public class MaintenanceRecordDTO
    {
        public Guid Id { get; set; }
        public Guid MotorcycleId { get; set; }
        public Guid UserMaintenanceId { get; set; }
        public DateTime PerformedAt { get; set; }
        public int? PerformedKm { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MaintenanceName { get; set; } = string.Empty;
    }
}
