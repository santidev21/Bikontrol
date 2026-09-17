using Bikontrol.Persistence.Entities;

namespace Bikontrol.Tests;

public class UserEntityTests
{
    [Theory]
    [InlineData("  Test@Bikontrol.com  ", "test@bikontrol.com")]
    [InlineData("USER@DOMAIN.COM", "user@domain.com")]
    [InlineData("already@lower.com", "already@lower.com")]
    public void Constructor_ShouldNormalizeEmail(string input, string expected)
    {
        var user = new User(input, "Name", "hash");

        Assert.Equal(expected, user.Email);
    }

    [Fact]
    public void NormalizeEmail_ShouldTrimAndLowercase()
    {
        Assert.Equal("a@b.com", User.NormalizeEmail("  A@B.COM "));
        Assert.Equal(string.Empty, User.NormalizeEmail(null));
    }
}
