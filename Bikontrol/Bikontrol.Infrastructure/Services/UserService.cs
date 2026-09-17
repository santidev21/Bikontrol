using AutoMapper;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Persistence.Entities;
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
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _current;

        public UserService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher<User> passwordHasher,
            IMapper mapper,
            ICurrentUserService current)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
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
                throw new ValidationException("Tu cuenta usa inicio de sesión con Google y no tiene contraseña para cambiar.");

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification != PasswordVerificationResult.Success)
                throw new ValidationException("La contraseña actual no es correcta.");

            user.UpdatePassword(_passwordHasher.HashPassword(user, request.NewPassword));
            user.ClearResetPasswordToken();
            // Cambiar la contraseña cierra todas las sesiones: los refresh tokens
            // previos dejan de ser válidos (se persisten en el mismo SaveChanges).
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var user = await _userRepository.GetByIdAsync(_current.UserId);
            if (user is null)
                throw new NotFoundException("Usuario no encontrado.");
            return user;
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }

        private ProfileDTO ToProfile(User user)
        {
            var dto = _mapper.Map<ProfileDTO>(user);
            dto.HasPassword = user.HasPassword;
            return dto;
        }
    }
}
