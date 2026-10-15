using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Text;

namespace Bikontrol.Infrastructure.Authentication
{
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int ExpiresInSeconds
        {
            get
            {
                var minutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var m) ? m : 30;
                return Math.Max(1, minutes) * 60;
            }
        }

        public string GenerateToken(Guid userId, string email, string fullName, string role = Persistence.Entities.UserRole.User)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("fullName", fullName),
                new Claim("role", string.IsNullOrWhiteSpace(role) ? Persistence.Entities.UserRole.User : role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // JsonWebTokenHandler is the recommended, faster handler (the legacy
            // JwtSecurityTokenHandler is on the way out). The claim names are
            // unchanged, so validation (MapInboundClaims=false) is unaffected.
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(ExpiresInSeconds),
                SigningCredentials = creds
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
