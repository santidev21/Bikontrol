using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Bikontrol.Infrastructure.Authentication
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        public const string SecurityStampClaim = "sstamp";

        private readonly JwtOptions _options;

        public JwtTokenGenerator(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public int ExpiresInSeconds => _options.ExpireMinutes * 60;

        public int RefreshExpireDays => _options.RefreshExpireDays;

        public string GenerateToken(Guid userId, string email, string fullName, string role = Domain.Entities.UserRole.User, Guid? securityStamp = null)
        {
            // The kid lets the validator pick the right key while a rotation is
            // in flight (old tokens keep verifying against Jwt:PreviousKeys).
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)) { KeyId = _options.KeyId };
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("fullName", fullName),
                new Claim("role", string.IsNullOrWhiteSpace(role) ? Domain.Entities.UserRole.User : role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (securityStamp is { } stamp)
                claims.Add(new Claim(SecurityStampClaim, stamp.ToString("N")));

            // JsonWebTokenHandler is the recommended, faster handler (the legacy
            // JwtSecurityTokenHandler is on the way out). The claim names are
            // unchanged, so validation (MapInboundClaims=false) is unaffected.
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(ExpiresInSeconds),
                SigningCredentials = creds
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
