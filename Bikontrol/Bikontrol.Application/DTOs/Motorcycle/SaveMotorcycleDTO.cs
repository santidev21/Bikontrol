using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Motorcycle
{
    public class SaveMotorcycleDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La marca es obligatoria.")]
        public string Brand { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio.")]
        public int Year { get; set; }

        [Required(ErrorMessage = "El apodo es obligatorio.")]
        public string Nickname { get; set; } = string.Empty;

        [Range(0, 1_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 1,000,000 km.")]
        public int Km { get; set; }

        [Range(0, 2300, ErrorMessage = "El cilindraje debe estar entre 0 y 2300 cc.")]
        public int Displacement { get; set; }

        [Required(ErrorMessage = "La placa es obligatoria.")]
        public string Plate { get; set; } = string.Empty;
    }
}
