using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Motorcycle
{
    public class CreateMotorcycleDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int Km { get; set; }
        public int Displacement { get; set; }
        public string Plate { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
