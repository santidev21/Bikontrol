namespace Bikontrol.Application.DTOs.Maintenance
{
    public class UpcomingMaintenanceDTO
    {
        public Guid UserMaintenanceId { get; set; }
        public Guid MotorcycleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TrackingType { get; set; } = "Km";
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
        public int RemainingKm { get; set; }
        public int RemainingDays { get; set; }
        public int LifePercent { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime? LastPerformedAt { get; set; }
        public int? LastPerformedKm { get; set; }
    }
}
