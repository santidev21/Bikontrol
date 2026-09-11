using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "El token de Google es obligatorio.")]
        public string IdToken { get; set; } = string.Empty;
    }
}
