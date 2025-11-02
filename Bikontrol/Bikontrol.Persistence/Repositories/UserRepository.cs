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
        private readonly IMapper _mapper;

        public UserRepository(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return _mapper.Map<User?>(entity);
        }

        public async Task AddAsync(User user)
        {
            var entity = _mapper.Map<UserEntity>(user);
            await _context.Users.AddAsync(entity);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
