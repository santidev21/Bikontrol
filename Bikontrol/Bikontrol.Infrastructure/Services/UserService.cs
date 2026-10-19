using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ICurrentUserService _current;

        public UserService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<User> passwordHasher,
            ICurrentUserService current)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _current = current;
        }

        public async Task<ProfileDTO> GetMeAsync()
        {
            var user = await GetCurrentUserAsync();
            return ToProfile(user);
        }

        public async Task<ProfileDTO> UpdateProfileAsync(UpdateProfileRequest request)
        {
            EnsureCanWrite();
            var user = await GetCurrentUserAsync();
            user.UpdateFullName(request.FullName);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return ToProfile(user);
        }

        public async Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            EnsureCanWrite();
            var user = await GetCurrentUserAsync();

            if (!user.HasPassword)
                throw new ValidationException("Tu cuenta usa inicio de sesión con Google y no tiene contraseña para cambiar.", "google_no_password");

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification != PasswordVerificationResult.Success)
                throw new ValidationException("La contraseña actual no es correcta.", "current_password_incorrect");

            user.UpdatePassword(_passwordHasher.HashPassword(user, request.NewPassword));
            user.ClearResetPasswordToken();
            // Cambiar la contraseña cierra todas las sesiones: los refresh tokens
            // previos dejan de ser válidos (se persisten en el mismo SaveChanges).
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<ProfileDTO> UpdateRemindersAsync(UpdateRemindersRequest request)
        {
            EnsureCanWrite();
            var user = await GetCurrentUserAsync();
            user.SetRemindersEnabled(request.Enabled);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return ToProfile(user);
        }

        public async Task<ProfileDTO> UpdateBudgetAsync(UpdateBudgetRequest request)
        {
            EnsureCanWrite();
            var user = await GetCurrentUserAsync();
            user.SetMaintenanceBudget(request.MonthlyMaintenanceBudget, request.AnnualMaintenanceBudget);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return ToProfile(user);
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var user = await _userRepository.GetByIdAsync(_current.UserId);
            if (user is null)
                throw new NotFoundException("Usuario no encontrado.", "user_not_found");
            return user;
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.", "demo_read_only");
        }

        private ProfileDTO ToProfile(User user)
        {
            var dto = user.ToProfileDTO();
            dto.HasPassword = user.HasPassword;
            return dto;
        }
    }
}
