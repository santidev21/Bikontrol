using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class FollowDefaultRequest
    {
        public Guid MotorcycleId { get; set; }
        public Guid DefaultId { get; set; }

        [Range(1, 1_000_000, ErrorMessage = "El intervalo de kilometraje debe estar entre 1 y 1,000,000 km.")]
        public int? KmInterval { get; set; }

        [Range(1, 5_200, ErrorMessage = "El intervalo de tiempo debe estar entre 1 y 5,200 semanas.")]
        public int? TimeIntervalWeeks { get; set; }

        [Required(ErrorMessage = "El tipo de seguimiento es obligatorio.")]
        [RegularExpression("^(Km|Time)$", ErrorMessage = "El tipo de seguimiento debe ser 'Km' o 'Time'.")]
        public string TrackingType { get; set; } = "Km";
    }
}
