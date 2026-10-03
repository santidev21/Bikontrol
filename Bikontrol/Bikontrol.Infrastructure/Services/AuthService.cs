using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Shared.Exceptions;
using Bikontrol.Persistence;
using Bikontrol.Persistence.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private const int ResetPasswordTokenHours = 2;
        private const int EmailConfirmationTokenHours = 24;

        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<User> passwordHasher,
            JwtTokenGenerator jwtTokenGenerator,
            IEmailSender emailSender,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailSender = emailSender;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new AuthException("El usuario ya existe.", 409);

            var user = _mapper.Map<User>(request);
            user.SetPasswordHash(_passwordHasher.HashPassword(null!, request.Password));

            await _userRepository.AddAsync(user);
            var response = _mapper.Map<RegisterResponse>(user);

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

            response.Token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role);
            response.RefreshToken = refreshToken;
            response.ExpiresIn = _jwtTokenGenerator.ExpiresInSeconds;
            return response;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
                throw new AuthException("El correo o contraseña son inválidos.");

            // Never run the hasher while locked out, so a locked account cannot
            // be used to keep guessing (and cannot spend more CPU).
            if (IsLockoutEnabled() && user.IsLockedOut)
                throw new AuthException(
                    "La cuenta está temporalmente bloqueada por demasiados intentos fallidos. Intenta de nuevo más tarde.",
                    429);

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
            {
                if (IsLockoutEnabled())
                {
                    user.RegisterFailedLogin(GetMaxFailedAttempts(), GetLockoutDuration());
                    await _userRepository.SaveChangesAsync();
                }

                throw new AuthException("El correo o contraseña son inválidos.");
            }

            if (IsEmailConfirmationRequired() && !user.IsEmailConfirmed)
                throw new AuthException(
                    "Debes confirmar tu correo antes de iniciar sesión. Revisá tu bandeja o pedí un nuevo enlace.",
                    403);

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
                throw new AuthException("El inicio de sesión con Google no está configurado.", 503);

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    });
            }
            catch (InvalidJwtException)
            {
                throw new AuthException("El token de Google no es válido.");
            }

            if (payload.EmailVerified != true)
                throw new AuthException("La cuenta de Google debe tener el email verificado.");

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
            var tokenHash = HashToken(request.RefreshToken);
            var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
            if (stored?.User == null || !stored.IsActive)
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.");

            stored.Revoke();
            var newRefreshToken = await IssueRefreshTokenAsync(stored.UserId);
            try
            {
                await _refreshTokenRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another concurrent refresh already rotated this token.
                // Report it as an expired session so the client logs out cleanly
                // instead of surfacing a 409 conflict.
                throw new AuthException("La sesión expiró. Inicia sesión de nuevo.");
            }

            return BuildLoginResponse(stored.User, newRefreshToken);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
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

            await _emailSender.SendAsync(user.Email, "Restablece tu contraseña de Bikontrol", body);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.ResetPasswordTokenHash))
                throw new AuthException("El enlace de recuperación no es válido o ya expiró.");

            if (user.ResetPasswordTokenExpires is null || user.ResetPasswordTokenExpires < DateTime.UtcNow)
                throw new AuthException("El enlace de recuperación expiró. Solicita uno nuevo.");

            var providedHash = HashToken(request.Token);
            if (!FixedTimeEquals(user.ResetPasswordTokenHash, providedHash))
                throw new AuthException("El enlace de recuperación no es válido.");

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
                throw new AuthException("El enlace de confirmación no es válido o ya expiró.");

            // Idempotent: confirming an already-confirmed account is a no-op.
            if (user.IsEmailConfirmed)
                return;

            if (string.IsNullOrWhiteSpace(user.EmailConfirmationTokenHash)
                || user.EmailConfirmationTokenExpires is null
                || user.EmailConfirmationTokenExpires < DateTime.UtcNow)
                throw new AuthException("El enlace de confirmación no es válido o ya expiró.");

            var providedHash = HashToken(request.Token);
            if (!FixedTimeEquals(user.EmailConfirmationTokenHash, providedHash))
                throw new AuthException("El enlace de confirmación no es válido.");

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
                throw new AuthException("El modo demo no está disponible.", 404);

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
                throw new AuthException("El usuario demo no está disponible.", 403);
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

        private async Task SendConfirmationEmailAsync(User user, string token)
        {
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
            var link = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
            var body =
                $"Hola {user.FullName},\n\n" +
                "Gracias por registrarte en Bikontrol.\n" +
                $"Confirmá tu correo con este enlace (expira en {EmailConfirmationTokenHours} horas):\n\n{link}\n\n" +
                "Si no creaste esta cuenta, ignorá este correo.";

            await _emailSender.SendAsync(user.Email, "Confirmá tu correo de Bikontrol", body);
        }

        private LoginResponse BuildLoginResponse(User user, string refreshToken)
        {
            var response = _mapper.Map<LoginResponse>(user);
            response.Token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName, user.Role);
            response.RefreshToken = refreshToken;
            response.ExpiresIn = _jwtTokenGenerator.ExpiresInSeconds;
            return response;
        }

        private async Task<string> IssueRefreshTokenAsync(Guid userId)
        {
            var rawToken = GenerateSecureToken();
            var days = int.TryParse(_configuration["Jwt:RefreshExpireDays"], out var parsedDays) ? parsedDays : 30;
            var entity = new RefreshToken(userId, HashToken(rawToken), DateTime.UtcNow.AddDays(Math.Max(1, days)));
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
