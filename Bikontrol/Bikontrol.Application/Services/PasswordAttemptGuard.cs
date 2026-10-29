using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Shares the login lockout counter with the endpoints that re-verify the
    /// current password (change password, delete account), so a stolen access
    /// token cannot be used to brute-force the password there.
    /// </summary>
    internal static class PasswordAttemptGuard
    {
        public static bool IsEnabled(IConfiguration? configuration) =>
            !bool.TryParse(configuration?["Lockout:Enabled"], out var enabled) || enabled;

        public static void EnsureNotLocked(User user, IConfiguration? configuration)
        {
            if (IsEnabled(configuration) && user.IsLockedOut)
                throw new AuthException(
                    "La cuenta está temporalmente bloqueada por demasiados intentos fallidos. Intenta de nuevo más tarde.",
                    429,
                    "account_locked");
        }

        public static void RegisterFailure(User user, IConfiguration? configuration)
        {
            if (!IsEnabled(configuration))
                return;

            var max = int.TryParse(configuration?["Lockout:MaxFailedAttempts"], out var m) && m > 0 ? m : 5;
            var minutes = int.TryParse(configuration?["Lockout:Minutes"], out var d) && d > 0 ? d : 15;
            user.RegisterFailedLogin(max, TimeSpan.FromMinutes(minutes));
        }
    }
}
