using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikontrol.Domain.Authentication.Entities;

namespace Bikontrol.Application.Interfaces
{
    public interface IUserRepository
    {
        Task Add(User user);
        Task<User?> GetByEmail(string email);
    }
}
