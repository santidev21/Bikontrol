namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// A photo/document attached to a maintenance record (e.g. an invoice or a
    /// photo of the part). The client sends a resized image data URL; when object
    /// storage is enabled the bytes live in the bucket under <see cref="StorageKey"/>
    /// and <see cref="DataUrl"/> is empty. Rows created before that keep the data
    /// URL in the database until the migration command moves them.
    /// </summary>
    public class MaintenanceRecordAttachment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MotorcycleMaintenanceRecordId { get; set; }

        /// <summary>JPEG/PNG/WebP data URL (validated + size-capped server-side). Empty once the image is in object storage.</summary>
        public string DataUrl { get; set; } = string.Empty;

        /// <summary>Object key in the bucket, or null while the image still lives in <see cref="DataUrl"/>.</summary>
        public string? StorageKey { get; set; }

        /// <summary>Decoded image size, used for the per-user storage quota.</summary>
        public int SizeBytes { get; set; }

        /// <summary>Optional, user-supplied label shown in the UI.</summary>
        public string? FileName { get; set; }

        public string ContentType { get; set; } = "image/jpeg";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MotorcycleMaintenanceRecord? Record { get; set; }
    }
}
