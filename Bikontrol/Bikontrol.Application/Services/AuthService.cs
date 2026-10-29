using Bikontrol.Application.Email;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Shared.Exceptions;
using Bikontrol.Domain.Entities;
using Bikontrol.Application.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int ResetPasswordTokenHours = 2;
        private const int EmailConfirmationTokenHours = 24;

        /// <summary>Two tabs may refresh with the same token within this window without it counting as theft.</summary>
        private static readonly TimeSpan RefreshReuseGrace = TimeSpan.FromSeconds(10);

        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<User> passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IGoogleTokenValidator googleTokenValidator,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _googleTokenValidator = googleTokenValidator;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing is not null)
            {
                // Anti-enumeration: an already-registered address must be
                // indistinguishable from a fresh one. Same response shape, an
                // email goes to the real owner, and nothing from the existing
                // account leaks back. Caveat: with EmailConfirmation:Required
                // disabled the fresh path issues a session immediately, so an
                // existing address can only be answered with
                // EmailConfirmationRequired=true — keep the default in prod.
                if (existing.IsEmailConfirmed)
                {
                    await SendAccountExistsEmailAsync(existing);
                }
                else
                {
                    // Still unconfirmed: they cannot log in yet, so resend the
                    // confirmation link instead of an "account exists" note.
                    await ResendConfirmationAsync(new ResendConfirmationRequest { Email = request.Email });
                }

                return BuildGenericRegisterResponse(request);
            }

            var user = new User(request.Email, request.FullName, string.Empty);
            user.SetPasswordHash(_passwordHasher.HashPassword(null!, request.Password));

            await _userRepository.AddAsync(user);
            var response = user.ToRegisterResponse();

            if (IsEmailConfirmationRequired())
            {
                // No session until the email is confirmed; send a confirmation link.
                var confirmationToken = GenerateSecureToken();
                user.SetEmailConfirmationToken(
                    HashToken(confirmationToken),
                    DateTime.UtcNow.AddHours(EmailConfirmationTokenHours));
                await _userRepository.SaveChangesAsync();

                await SendConfirmationEmailAsync(user, confirmationToken);

                response.EmailConfirmationRequired = true;
                return response;
            }

            // Verification disabled: treat the account as confirmed.
            user.MarkEmailConfirmed();
            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            response.Token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role, user.SecurityStamp);
            response.RefreshToken = refreshToken;
            response.ExpiresIn = _jwtTokenGenerator.ExpiresInSeconds;
            return response;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                // Spend the same CPU as a real verification so response time does
                // not reveal whether the address is registered.
                _passwordHasher.HashPassword(null!, dto.Password);
                throw new AuthException("El correo o contraseña son inválidos.", 401, "invalid_credentials");
            }

            // Never run the hasher while locked out, so a locked account cannot
            // be used to keep guessing (and cannot spend more CPU).
            if (IsLockoutEnabled() && user.IsLockedOut)
                throw new AuthException(
                    "La cuenta está temporalmente bloqueada por demasiados intentos fallidos. Intenta de nuevo más tarde.",
                    429,
                    "account_locked");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
            {
                if (IsLockoutEnabled())
                {
                    user.RegisterFailedLogin(GetMaxFailedAttempts(), GetLockoutDuration());
                    await _userRepository.SaveChangesAsync();
                }

                throw new AuthException("El correo o contraseña son inválidos.", 401, "invalid_credentials");
            }

            if (IsEmailConfirmationRequired() && !user.IsEmailConfirmed)
                throw new AuthException(
                    "Debes confirmar tu correo antes de iniciar sesión. Revisá tu bandeja o pedí un nuevo enlace.",
                    403,
                    "email_unconfirmed");

            // Successful login clears any previous failed-attempt counter.
            user.ResetAccessFailed();
            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            return BuildLoginResponse(user, refreshToken);
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new AuthException("El inicio de sesión con Google no está configurado.", 503, "google_not_configured");

            var payload = await _googleTokenValidator.ValidateAsync(request.IdToken, clientId);
            if (payload is null)
                throw new AuthException("El token de Google no es válido.", 401, "google_invalid_token");

            if (!payload.EmailVerified)
                throw new AuthException("La cuenta de Google debe tener el email verificado.", 401, "google_email_unverified");

            var user = await _userRepository.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                var fullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name;
                // No password is set for Google-only accounts; store a random hash so
                // the account cannot be accessed with a password until one is created
                // through the password-recovery flow.
                var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                user = new User(payload.Email, fullName, _passwordHasher.HashPassword(null!, randomPassword));
                user.SetAuthProvider("Google");
                user.MarkEmailConfirmed();
                await _userRepository.AddAsync(user);
            }
            else if (!user.IsEmailConfirmed)
            {
                // Google already verified ownership of this email address.
                user.MarkEmailConfirmed();
            }

            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            return BuildLoginResponse(user, refreshToken);
        }

        public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request)
        {
            // Cookie flow: an absent token means the session is simply gone.
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.", 401, "session_expired");

            var tokenHash = HashToken(request.RefreshToken);
            var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
            if (stored?.User == null)
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.", 401, "session_expired");

            // A token that was already rotated and is presented again (beyond the
            // grace window for two tabs refreshing at once) means it leaked:
            // kill the whole chain so neither party keeps a session.
            if (stored.IsReused(RefreshReuseGrace))
            {
                await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId);
                await _refreshTokenRepository.SaveChangesAsync();
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.", 401, "session_expired");
            }

            // Re-check the account: a refresh must not outlive a lockout or an
            // unconfirmed email.
            if (!stored.IsActive
                || (IsLockoutEnabled() && stored.User.IsLockedOut)
                || (IsEmailConfirmationRequired() && !stored.User.IsEmailConfirmed))
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.", 401, "session_expired");

            stored.Revoke();
            var newRefreshToken = await IssueRefreshTokenAsync(stored.UserId, stored.FamilyId);
            if (!await _refreshTokenRepository.TrySaveAsync())
            {
                // Another concurrent refresh already rotated this token.
                // Report it as an expired session so the client logs out cleanly
                // instead of surfacing a 409 conflict.
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.", 401, "session_expired");
            }

            return BuildLoginResponse(stored.User, newRefreshToken);
        }

        public async Task LogoutAsync(string? refreshToken)
        {
            // Best-effort: logout must never fail the user, and an unknown or
            // already-dead token is simply nothing to revoke.
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var stored = await _refreshTokenRepository.GetByTokenHashAsync(HashToken(refreshToken));
            if (stored is { IsActive: true })
            {
                stored.Revoke();
                await _refreshTokenRepository.SaveChangesAsync();
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // An unknown address returns instantly while a known one sends an
            // email; pad both to a floor so latency does not leak registration.
            var started = Stopwatch.StartNew();
            await ForgotPasswordCoreAsync(request);
            var floor = TimeSpan.FromMilliseconds(
                int.TryParse(_configuration["Auth:ForgotPasswordMinMs"], out var ms) && ms >= 0 ? ms : 600);
            var remaining = floor - started.Elapsed;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);
        }

        private async Task ForgotPasswordCoreAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            // Always succeed to avoid revealing whether the email is registered.
            if (user == null)
                return;

            var token = GenerateSecureToken();
            user.SetResetPasswordToken(HashToken(token), DateTime.UtcNow.AddHours(ResetPasswordTokenHours));
            await _userRepository.SaveChangesAsync();

            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
            var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
            var body =
                $"Hola {user.FullName},\n\n" +
                "Recibimos una solicitud para restablecer tu contraseña de Bikontrol.\n" +
                $"Usa este enlace para crear una nueva (expira en {ResetPasswordTokenHours} horas):\n\n{link}\n\n" +
                "Si no solicitaste esto, ignora este correo.";
            var html = EmailTemplate.Render(
                heading: "Restablece tu contraseña",
                paragraphs: new[]
                {
                    $"Hola {user.FullName}, recibimos una solicitud para restablecer tu contraseña de Bikontrol.",
                    $"El enlace expira en {ResetPasswordTokenHours} horas.",
                },
                buttonLabel: "Crear nueva contraseña",
                buttonUrl: link,
                footerLine: "Si no solicitaste esto, ignora este correo: tu contraseña no cambiará.");

            await _emailSender.SendAsync(new EmailMessage(user.Email, "Restablece tu contraseña de Bikontrol", body, html));
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.ResetPasswordTokenHash))
                throw new AuthException("El enlace de recuperación no es válido o ya expiró.", 401, "reset_invalid_token");

            if (user.ResetPasswordTokenExpires is null || user.ResetPasswordTokenExpires < DateTime.UtcNow)
                throw new AuthException("El enlace de recuperación expiró. Solicita uno nuevo.", 401, "reset_expired_token");

            var providedHash = HashToken(request.Token);
            if (!FixedTimeEquals(user.ResetPasswordTokenHash, providedHash))
                throw new AuthException("El enlace de recuperación no es válido.", 401, "reset_invalid_token");

            user.UpdatePassword(_passwordHasher.HashPassword(user, request.NewPassword));
            user.ClearResetPasswordToken();
            // A successful password reset unlocks the account.
            user.ResetAccessFailed();
            // Si era una cuenta Google, ahora tiene contraseña usable.
            user.SetAuthProvider(null);
            // Restablecer la contraseña cierra todas las sesiones existentes.
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
            await _userRepository.SaveChangesAsync();
        }

        public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new AuthException("El enlace de confirmación no es válido o ya expiró.", 401, "confirm_invalid_token");

            // Idempotent: confirming an already-confirmed account is a no-op.
            if (user.IsEmailConfirmed)
                return;

            if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash)
                || user.EmailConfirmationTokenExpires is null
                || user.EmailConfirmationTokenExpires < DateTime.UtcNow)
                throw new AuthException("El enlace de confirmación no es válido o ya expiró.", 401, "confirm_invalid_token");

            var providedHash = HashToken(request.Token);
            if (!FixedTimeEquals(user.EmailConfirmationTokenHash, providedHash))
                throw new AuthException("El enlace de confirmación no es válido.", 401, "confirm_invalid_token");

            user.MarkEmailConfirmed();
            await _userRepository.SaveChangesAsync();
        }

        public async Task ResendConfirmationAsync(ResendConfirmationRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            // Always succeed to avoid revealing whether the email is registered.
            if (user == null || user.IsEmailConfirmed)
                return;

            var token = GenerateSecureToken();
            user.SetEmailConfirmationToken(HashToken(token), DateTime.UtcNow.AddHours(EmailConfirmationTokenHours));
            await _userRepository.SaveChangesAsync();

            await SendConfirmationEmailAsync(user, token);
        }

        public async Task<LoginResponse> DemoLoginAsync()
        {
            if (!IsDemoEnabled())
                throw new AuthException("El modo demo no está disponible.", 404, "demo_disabled");

            var demoEmail = _configuration["DemoUser:Email"] ?? "demo@bikontrol.com";
            var demoName = _configuration["DemoUser:FullName"] ?? "Usuario Demo";

            var user = await _userRepository.GetByEmailAsync(demoEmail);
            if (user == null)
            {
                var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                user = new User(demoEmail, demoName, _passwordHasher.HashPassword(null!, randomPassword), UserRole.Demo);
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }
            else if (user.Role != UserRole.Demo)
            {
                // The configured demo email collides with a real account.
                // Never hand out that account through the anonymous demo endpoint.
                throw new AuthException("El usuario demo no está disponible.", 403, "demo_unavailable");
            }

            // The demo account is usable immediately; it is not a real inbox.
            if (!user.IsEmailConfirmed)
                user.MarkEmailConfirmed();

            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            return BuildLoginResponse(user, refreshToken);
        }

        /// <summary>
        /// Demo mode is opt-in: it must be explicitly enabled through
        /// <c>Demo:Enabled</c> (defaults to false, so production has no public
        /// demo tenant unless the operator turns it on).
        /// </summary>
        private bool IsDemoEnabled()
        {
            return bool.TryParse(_configuration["Demo:Enabled"], out var enabled) && enabled;
        }

        /// <summary>
        /// Email verification is required unless explicitly disabled. It fails
        /// secure: an absent <c>EmailConfirmation:Required</c> value means true,
        /// so a misconfigured production still blocks unverified logins.
        /// </summary>
        private bool IsEmailConfirmationRequired()
        {
            return !bool.TryParse(_configuration["EmailConfirmation:Required"], out var required) || required;
        }

        /// <summary>Account lockout is on unless explicitly disabled.</summary>
        private bool IsLockoutEnabled()
        {
            return !bool.TryParse(_configuration["Lockout:Enabled"], out var enabled) || enabled;
        }

        private int GetMaxFailedAttempts()
        {
            return int.TryParse(_configuration["Lockout:MaxFailedAttempts"], out var value) && value > 0
                ? value
                : 5;
        }

        private TimeSpan GetLockoutDuration()
        {
            return int.TryParse(_configuration["Lockout:Minutes"], out var minutes) && minutes > 0
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.FromMinutes(15);
        }

        /// <summary>
        /// Response for an email that is already registered: identical in shape
        /// to a fresh registration's confirmation-required response, with a
        /// random Id, the requested (not the stored) name and a User role, so
        /// the two are not distinguishable.
        /// </summary>
        private static RegisterResponse BuildGenericRegisterResponse(RegisterRequest request)
        {
            return new RegisterResponse
            {
                Id = Guid.NewGuid(),
                Email = User.NormalizeEmail(request.Email),
                FullName = request.FullName,
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmationRequired = true
            };
        }

        private async Task SendAccountExistsEmailAsync(User user)
        {
            var body =
                $"Hola {user.FullName},\n\n" +
                "Recibimos un intento de registro con tu correo en Bikontrol, pero ya existe una cuenta con esta dirección.\n\n" +
                "Si fuiste vos, iniciá sesión con tu contraseña (o restablecela si no la recordás).\n" +
                "Si no fuiste vos, ignorá este correo: nadie pudo acceder a tu cuenta.\n";

            var html = EmailTemplate.Render(
                heading: "Ya tenés una cuenta en Bikontrol",
                paragraphs: new[]
                {
                    $"Hola {user.FullName}, recibimos un intento de registro con tu correo en Bikontrol, pero ya existe una cuenta con esta dirección.",
                    "Si fuiste vos, iniciá sesión con tu contraseña (o restablecela si no la recordás).",
                },
                footerLine: "Si no fuiste vos, ignorá este correo: nadie pudo acceder a tu cuenta.");

            await _emailSender.SendAsync(new EmailMessage(user.Email, "Ya tenés una cuenta en Bikontrol", body, html));
        }

        private async Task SendConfirmationEmailAsync(User user, string token)
        {
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
            var link = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
            var body =
                $"Hola {user.FullName},\n\n" +
                "Gracias por registrarte en Bikontrol.\n" +
                $"Confirmá tu correo con este enlace (expira en {EmailConfirmationTokenHours} horas):\n\n{link}\n\n" +
                "Si no creaste esta cuenta, ignorá este correo.";

            var html = EmailTemplate.Render(
                heading: "Confirmá tu correo",
                paragraphs: new[]
                {
                    $"Hola {user.FullName}, gracias por registrarte en Bikontrol.",
                    $"Confirmá tu correo con el botón de abajo (el enlace expira en {EmailConfirmationTokenHours} horas).",
                },
                buttonLabel: "Confirmar correo",
                buttonUrl: link,
                footerLine: "Si no creaste esta cuenta, ignorá este correo.");

            await _emailSender.SendAsync(new EmailMessage(user.Email, "Confirmá tu correo de Bikontrol", body, html));
        }

        private LoginResponse BuildLoginResponse(User user, string refreshToken)
        {
            var response = user.ToLoginResponse();
            response.Token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role, user.SecurityStamp);
            response.RefreshToken = refreshToken;
            response.ExpiresIn = _jwtTokenGenerator.ExpiresInSeconds;
            return response;
        }

        private async Task<string> IssueRefreshTokenAsync(Guid userId, Guid? familyId = null)
        {
            var rawToken = GenerateSecureToken();
            var entity = new RefreshToken(userId, HashToken(rawToken), DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshExpireDays), familyId);
            await _refreshTokenRepository.AddAsync(entity);
            return rawToken;
        }

        private static string GenerateSecureToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.ASCII.GetBytes(left);
            var rightBytes = Encoding.ASCII.GetBytes(right);
            return leftBytes.Length == rightBytes.Length
                && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
