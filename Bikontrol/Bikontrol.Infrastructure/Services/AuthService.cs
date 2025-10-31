using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Bikontrol.Persistence;
using Bikontrol.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            // Check existing email
            if (_context.Users.Any(u => u.Email == request.Email))
                throw new Exception("Email already registered.");

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new RegisterResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt
            };
        }
    }

}
