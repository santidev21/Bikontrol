using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Shared.Exceptions
{
    public class AuthException : Exception
    {
        public int StatusCode { get; }

        public AuthException(string message, int statusCode = 401)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
