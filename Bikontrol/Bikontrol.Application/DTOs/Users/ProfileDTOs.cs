using System;
using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.DTOs.Users
{
    public class ProfileDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// False en cuentas creadas vía Google (no tienen contraseña que cambiar).
        /// </summary>
        public bool HasPassword { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
        public string FullName { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
