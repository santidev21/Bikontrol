using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        // Optional: the browser flow presents the token through the HttpOnly
        // cookie; the body field stays for scripts and tooling.
        public string? RefreshToken { get; set; }
    }

    public class LogoutRequest
    {
        // Fallback for non-cookie clients (scripts); browser logout uses the cookie.
        public string? RefreshToken { get; set; }
    }
}
