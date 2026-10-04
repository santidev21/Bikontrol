using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class AddAttachmentRequest
    {
        /// <summary>Resized image as a data URL (JPEG/PNG/WebP).</summary>
        [Required(ErrorMessage = "La imagen es obligatoria.")]
        [MaxLength(1_400_000, ErrorMessage = "La imagen es demasiado grande.")]
        public string DataUrl { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string? FileName { get; set; }
    }

    public class MaintenanceAttachmentDTO
    {
        public Guid Id { get; set; }
        public Guid RecordId { get; set; }
        public string DataUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string ContentType { get; set; } = "image/jpeg";
        public DateTime CreatedAt { get; set; }
    }
}
