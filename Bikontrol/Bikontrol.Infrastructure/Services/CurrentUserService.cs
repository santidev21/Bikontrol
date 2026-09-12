using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; }
        public string Role { get; }
        public bool IsDemo => Role == Persistence.Entities.UserRole.Demo;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                UserId = userId;

            Role = user?.FindFirst("role")?.Value ?? Persistence.Entities.UserRole.User;
        }
    }
}
