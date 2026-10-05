using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;

namespace Bikontrol.Application.DTOs.Users
{
    /// <summary>
    /// Full export of the current user's data ("download my data", GDPR-style).
    /// Structured JSON: profile plus every motorcycle, maintenance, record and
    /// attachment the user owns.
    /// </summary>
    public class UserDataExportDTO
    {
        public DateTime ExportedAt { get; set; }

        public ProfileDTO Profile { get; set; } = new();

        public List<MotorcycleDTO> Motorcycles { get; set; } = new();

        public List<MaintenanceDTO> Maintenances { get; set; } = new();

        public List<MaintenanceRecordDTO> MaintenanceRecords { get; set; } = new();

        public List<MaintenanceAttachmentDTO> Attachments { get; set; } = new();
    }
}
