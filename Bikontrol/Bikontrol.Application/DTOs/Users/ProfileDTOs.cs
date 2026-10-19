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

        /// <summary>Preferencia de recordatorios de mantenimiento (email/push).</summary>
        public bool RemindersEnabled { get; set; }

        /// <summary>Presupuesto mensual de mantenimiento (opcional).</summary>
        public decimal? MonthlyMaintenanceBudget { get; set; }

        /// <summary>Presupuesto anual de mantenimiento (opcional).</summary>
        public decimal? AnnualMaintenanceBudget { get; set; }
    }

    public class UpdateRemindersRequest
    {
        public bool Enabled { get; set; }
    }

    /// <summary>Presupuestos de mantenimiento del usuario (null desactiva el presupuesto).</summary>
    public class UpdateBudgetRequest
    {
        [Range(0, 100_000_000, ErrorMessage = "El presupuesto mensual debe estar entre 0 y 100,000,000.")]
        public decimal? MonthlyMaintenanceBudget { get; set; }

        [Range(0, 100_000_000, ErrorMessage = "El presupuesto anual debe estar entre 0 y 100,000,000.")]
        public decimal? AnnualMaintenanceBudget { get; set; }
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
        [MaxLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Confirms an irreversible account deletion. <c>Confirmation</c> must be the
    /// exact word ELIMINAR; <c>Password</c> is required for password accounts.
    /// </summary>
    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "Escribe ELIMINAR para confirmar.")]
        [MaxLength(20)]
        public string Confirmation { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? Password { get; set; }
    }
}
