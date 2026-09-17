using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Motorcycle
{
    public class AddKmHistoryRequest
    {
        [Range(0, 1_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 1,000,000 km.")]
        public int Km { get; set; }
    }
}
