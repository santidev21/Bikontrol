using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Bikontrol.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void CurrentUserService_ShouldParseUserIdAndRoleFromClaims()
    {
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("role", "Demo")
        }));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        Assert.Equal(userId, service.UserId);
        Assert.Equal("Demo", service.Role);
        Assert.True(service.IsDemo);
    }

    [Fact]
    public void CurrentUserService_WhenNoRole_ShouldDefaultToUser()
    {
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        Assert.Equal("User", service.Role);
        Assert.False(service.IsDemo);
    }

    [Fact]
    public void CurrentUserService_WhenNoUser_ShouldDefaultToEmpty()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var service = new CurrentUserService(accessor);

        Assert.Equal(Guid.Empty, service.UserId);
        Assert.Equal("User", service.Role);
    }
}
