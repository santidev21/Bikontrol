namespace Bikontrol.Application.DTOs.Maintenance
{
    public class CreateMaintenanceRecordRequest
    {
        public Guid MotorcycleId { get; set; }
        public Guid UserMaintenanceId { get; set; }
        public DateTime PerformedAt { get; set; }
        public int? PerformedKm { get; set; }
    }
}
