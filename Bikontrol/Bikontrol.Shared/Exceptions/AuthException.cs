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

        /// <summary>
        /// Stable machine code the frontend can translate (null falls back to
        /// "auth_error" in the error envelope).
        /// </summary>
        public string? Code { get; }

        public AuthException(string message, int statusCode = 401, string? code = null)
            : base(message)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }
}
