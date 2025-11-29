namespace Bikontrol.Application.DTOs.Maintenance
{
    public class MaintenanceDTO
    {
        public Guid Id { get; set; }
        public Guid? BaseTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSystem { get; set; }
    }
}
