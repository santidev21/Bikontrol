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
        /// Anti fuerza bruta: intentos fallidos consecutivos de login y, si está
        /// bloqueada, hasta cuándo (lockout temporal).
        /// </summary>
        public int AccessFailedCount { get; private set; }
        public DateTime? LockoutEnd { get; private set; }

        /// <summary>
        /// Preferencia de recordatorios de mantenimiento (email/push). Por
        /// defecto activados; el usuario puede desactivarlos.
        /// </summary>
        public bool RemindersEnabled { get; private set; } = true;

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

        /// <summary>True cuando la cuenta está bloqueada (lockout temporal vigente).</summary>
        public bool IsLockedOut => LockoutEnd is { } until && until > DateTime.UtcNow;

        /// <summary>
        /// Registra un login fallido y bloquea la cuenta al alcanzar el máximo.
        /// </summary>
        public void RegisterFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration)
        {
            AccessFailedCount++;
            if (AccessFailedCount >= Math.Max(1, maxFailedAttempts))
            {
                // Relative to the current lockout, if any, so re-applying it does
                // not thrash the column when concurrent attempts lock the account.
                LockoutEnd = (LockoutEnd ?? DateTime.UtcNow).Add(lockoutDuration);
                AccessFailedCount = 0;
            }
        }

        /// <summary>Limpia el lockout tras un login exitoso o un desbloqueo manual.</summary>
        public void ResetAccessFailed()
        {
            AccessFailedCount = 0;
            LockoutEnd = null;
        }

        /// <summary>Activa o desactiva los recordatorios de mantenimiento.</summary>
        public void SetRemindersEnabled(bool enabled)
        {
            RemindersEnabled = enabled;
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

        /// <summary>
        /// Scrambles the account's personal data while keeping the row, so the
        /// append-only audit trail keeps a stable owner reference. The email is
        /// replaced with a unique tombstone, freeing the original address for a
        /// future registration, and the password hash is replaced with an
        /// unusable value. Owned data is purged separately.
        /// </summary>
        public void Anonymize()
        {
            Email = $"deleted+{Id}@bikontrol.local";
            FullName = "Cuenta eliminada";
            PasswordHash = $"deleted:{Guid.NewGuid():N}";
            Role = UserRole.User;
            AuthProvider = null;
            ResetPasswordTokenHash = null;
            ResetPasswordTokenExpires = null;
            EmailConfirmationTokenHash = null;
            EmailConfirmationTokenExpires = null;
            EmailConfirmedAt = null;
            AccessFailedCount = 0;
            LockoutEnd = null;
            RemindersEnabled = false;
        }
    }
}
