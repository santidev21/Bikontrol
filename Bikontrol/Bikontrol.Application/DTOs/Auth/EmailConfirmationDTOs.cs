using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Auth
{
    public class ConfirmEmailRequest
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; } = string.Empty;
    }

    public class ResendConfirmationRequest
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
