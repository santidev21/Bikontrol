using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Shared.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message, string? code = null)
            : base(message)
        {
            Code = code;
        }

        /// <summary>Stable machine code the frontend can translate (null falls back to "forbidden").</summary>
        public string? Code { get; }
    }
}
