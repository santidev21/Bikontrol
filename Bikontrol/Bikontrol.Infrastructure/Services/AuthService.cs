using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Exceptions;
using Bikontrol.Persistence;
using Bikontrol.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            JwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new AuthException("El usuario ya existe.", 409);

            var user = new User(
                email: request.Email,
                fullName: request.FullName,
                passwordHash: _passwordHasher.HashPassword(null!, request.Password)
            );

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName);

            return new RegisterResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                Token = token
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
                throw new AuthException("El correo o contraseña son inválidos.", 401);

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                throw new AuthException("El correo o contraseña son inválidos.", 401);

            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.FullName);

            return new LoginResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }
    }
}
