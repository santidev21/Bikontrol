using AutoMapper;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalized = User.NormalizeEmail(email);
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalized = User.NormalizeEmail(email);
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == normalized);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
