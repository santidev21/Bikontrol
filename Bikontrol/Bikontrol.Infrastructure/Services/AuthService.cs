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
            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            var response = _mapper.Map<RegisterResponse>(user);
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

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                throw new AuthException("El correo o contraseña son inválidos.");

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
                await _userRepository.AddAsync(user);
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
            await _refreshTokenRepository.SaveChangesAsync();

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
            await _userRepository.SaveChangesAsync();
        }

        public async Task<LoginResponse> DemoLoginAsync()
        {
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

            var refreshToken = await IssueRefreshTokenAsync(user.Id);
            await _userRepository.SaveChangesAsync();

            return BuildLoginResponse(user, refreshToken);
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
