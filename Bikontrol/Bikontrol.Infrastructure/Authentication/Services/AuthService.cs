using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Bikontrol.Application.Authentication.DTOs;
using Bikontrol.Application.Authentication.Interfaces;
using Bikontrol.Application.Interfaces;
using Bikontrol.Domain.Authentication.Entities;
using Bikontrol.Infrastructure.Authentication.Exceptions;

namespace Bikontrol.Infrastructure.Authentication.Services
{
    public class AuthService : IAuthService
    {
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtService;
        public AuthService(
            IPasswordHasherService passwordHasherService,
            IUserRepository userRepository,
            IJwtTokenGenerator jwtService)
        {
            _passwordHasherService = passwordHasherService;
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            var existingUser = await _userRepository.GetByEmail(request.Email);
            if (existingUser != null)
            {
                throw new EmailAlreadyExistsException(request.Email);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = _passwordHasherService.Hash(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.Add(user); 

            var token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                FullName = $"{user.Name} {user.LastName}",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmail(request.Email);

            if (user is null)
            {
                throw new InvalidCredentialsException();
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new InvalidCredentialsException();
            }

            var token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                FullName = $"{user.Name} {user.LastName}",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

    }
}
