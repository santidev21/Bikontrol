using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class CreateMaintenanceRecordRequest
    {
        public Guid MotorcycleId { get; set; }
        public Guid UserMaintenanceId { get; set; }

        [Required(ErrorMessage = "La fecha de realización es obligatoria.")]
        public DateTime PerformedAt { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 1,000,000 km.")]
        public int? PerformedKm { get; set; }
    }
}
