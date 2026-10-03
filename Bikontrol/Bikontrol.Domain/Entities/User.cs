using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public string Role { get; private set; } = UserRole.User;
        public string? ResetPasswordTokenHash { get; private set; }
        public DateTime? ResetPasswordTokenExpires { get; private set; }

        /// <summary>
        /// Cuando no es null, el email fue verificado. Las cuentas creadas antes
        /// de esta feature se marcan al migrar; las nuevas quedan pendientes
        /// hasta confirmar (salvo Google, cuyo email ya viene verificado).
        /// </summary>
        public DateTime? EmailConfirmedAt { get; private set; }
        public string? EmailConfirmationTokenHash { get; private set; }
        public DateTime? EmailConfirmationTokenExpires { get; private set; }

        /// <summary>
        /// Origen de la cuenta: null/"Email" = registro con contraseña,
        /// "Google" = creada vía Google (sin contraseña usable).
        /// </summary>
        public string? AuthProvider { get; private set; }
        public IList<Motorcycle> Motorcycles { get; set; } = new List<Motorcycle>();

        private User() { }

        public User(string email, string fullName, string passwordHash, string? role = null)
        {
            Id = Guid.NewGuid();
            Email = NormalizeEmail(email);
            FullName = fullName;
            PasswordHash = passwordHash;
            Role = string.IsNullOrWhiteSpace(role) ? UserRole.User : role;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// El email se guarda y se compara normalizado (trim + minúsculas) para
        /// evitar cuentas duplicadas por diferencias de mayúsculas/espacios.
        /// </summary>
        public static string NormalizeEmail(string? email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        public void SetPasswordHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ValidationException("La contraseña no puede estar vacia.");

            PasswordHash = hash;
        }

        // Simple domain validation
        public void UpdatePassword(string newHash)
        {
            PasswordHash = newHash ?? throw new ArgumentNullException(nameof(newHash));
        }

        public void SetResetPasswordToken(string tokenHash, DateTime expiresAt)
        {
            ResetPasswordTokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
            ResetPasswordTokenExpires = expiresAt;
        }

        public void ClearResetPasswordToken()
        {
            ResetPasswordTokenHash = null;
            ResetPasswordTokenExpires = null;
        }

        public bool IsEmailConfirmed => EmailConfirmedAt is not null;

        public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAt)
        {
            EmailConfirmationTokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
            EmailConfirmationTokenExpires = expiresAt;
        }

        public void ClearEmailConfirmationToken()
        {
            EmailConfirmationTokenHash = null;
            EmailConfirmationTokenExpires = null;
        }

        /// <summary>Marca el email como verificado y descarta el token pendiente.</summary>
        public void MarkEmailConfirmed()
        {
            EmailConfirmedAt = DateTime.UtcNow;
            ClearEmailConfirmationToken();
        }

        public void SetAuthProvider(string? provider)
        {
            AuthProvider = string.IsNullOrWhiteSpace(provider) ? null : provider;
        }

        public void UpdateFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ValidationException("El nombre no puede estar vacio.");

            FullName = fullName.Trim();
        }

        public bool HasPassword => AuthProvider != "Google";

        public bool IsDemo => Role == UserRole.Demo;
    }
}
