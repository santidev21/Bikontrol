using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikontrol.Application.Authentication.Interfaces;
using Bikontrol.Domain.Authentication.Entities;

namespace Bikontrol.Persistence.Authentication
{
    public class UserRepository : IUserRepository
    {
        private readonly BikontrolDbContext _context;

        public UserRepository(BikontrolDbContext context)
        {
            _context = context;
        }

        public User? GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }
    }
}
