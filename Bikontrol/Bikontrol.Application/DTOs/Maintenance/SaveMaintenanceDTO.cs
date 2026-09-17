using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class SaveMaintenanceDTO
    {
        public Guid MotorcycleId { get; set; }
        public Guid? BaseTypeId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
        public string? Description { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "El intervalo de kilometraje debe estar entre 1 y 1,000,000 km.")]
        public int? KmInterval { get; set; }

        [Range(1, 5_200, ErrorMessage = "El intervalo de tiempo debe estar entre 1 y 5,200 semanas.")]
        public int? TimeIntervalWeeks { get; set; }

        [Required(ErrorMessage = "El tipo de seguimiento es obligatorio.")]
        [RegularExpression("^(Km|Time)$", ErrorMessage = "El tipo de seguimiento debe ser 'Km' o 'Time'.")]
        public string TrackingType { get; set; } = "Km";
    }
}
