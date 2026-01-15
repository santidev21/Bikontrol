namespace Bikontrol.Application.DTOs.Maintenance
{
    public class SaveMaintenanceDTO
    {
        public Guid? BaseTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
        public string TrackingType { get; set; } = "Km";
    }
}