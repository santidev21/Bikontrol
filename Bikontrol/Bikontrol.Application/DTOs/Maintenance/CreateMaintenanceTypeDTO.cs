namespace Bikontrol.Application.DTOs.Maintenance
{
    public class CreateMaintenanceTypeDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? KmInterval { get; set; }
        public int? TimeIntervalWeeks { get; set; }
    }
}