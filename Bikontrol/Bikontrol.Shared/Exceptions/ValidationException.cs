using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Shared.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(string message, string? code = null)
            : base(message)
        {
            Code = code;
        }

        /// <summary>Stable machine code the frontend can translate (null falls back to "validation").</summary>
        public string? Code { get; }
    }
}
