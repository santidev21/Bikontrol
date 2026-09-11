using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "El refresh token es obligatorio.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
