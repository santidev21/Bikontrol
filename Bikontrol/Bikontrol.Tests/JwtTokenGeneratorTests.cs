using Bikontrol.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bikontrol.Tests;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldIncludeExpectedClaimsAndConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "Bikontrol",
                ["Jwt:Audience"] = "Bikontrol.Tests"
            })
            .Build();

        var generator = new JwtTokenGenerator(config);
        var userId = Guid.NewGuid();

        var token = generator.GenerateToken(userId, "user@bikontrol.com", "Test User");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("Bikontrol", jwt.Issuer);
        Assert.Contains("Bikontrol.Tests", jwt.Audiences);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("user@bikontrol.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == "fullName").Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(29));
        Assert.True(jwt.ValidTo < DateTime.UtcNow.AddMinutes(31));
    }
}
