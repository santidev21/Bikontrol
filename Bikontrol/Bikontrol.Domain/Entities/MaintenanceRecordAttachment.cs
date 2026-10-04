namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// A photo/document attached to a maintenance record (e.g. an invoice or a
    /// photo of the part). Stored as a resized image data URL, like motorcycle
    /// images, so no file storage is needed.
    /// </summary>
    public class MaintenanceRecordAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MotorcycleMaintenanceRecordId { get; set; }

        /// <summary>JPEG/PNG/WebP data URL (validated + size-capped server-side).</summary>
        public string DataUrl { get; set; } = string.Empty;

        /// <summary>Optional, user-supplied label shown in the UI.</summary>
        public string? FileName { get; set; }

        public string ContentType { get; set; } = "image/jpeg";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MotorcycleMaintenanceRecord? Record { get; set; }
    }
}
