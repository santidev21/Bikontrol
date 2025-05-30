using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Authentication.Interfaces
{
    public interface IPasswordHasherService
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
