using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Maintenance
{
    public class FollowDefaultRequest
    {
        public Guid MotorcycleId { get; set; }
        public Guid DefaultId { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "El intervalo de kilometraje no puede ser negativo.")]
        public int? KmInterval { get; set; }

        [Range(0, 5_200, ErrorMessage = "El intervalo de tiempo no puede ser negativo.")]
        public int? TimeIntervalWeeks { get; set; }

        [Required(ErrorMessage = "El tipo de seguimiento es obligatorio.")]
        [RegularExpression("^(Km|Time)$", ErrorMessage = "El tipo de seguimiento debe ser 'Km' o 'Time'.")]
        public string TrackingType { get; set; } = "Km";
    }
}
