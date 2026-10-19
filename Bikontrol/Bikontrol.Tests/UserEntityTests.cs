using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

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

    [Fact]
    public void SetMaintenanceBudget_ShouldStoreValues()
    {
        var user = new User("a@b.com", "Name", "hash");

        user.SetMaintenanceBudget(100m, 1000m);

        Assert.Equal(100m, user.MonthlyMaintenanceBudget);
        Assert.Equal(1000m, user.AnnualMaintenanceBudget);
    }

    [Fact]
    public void SetMaintenanceBudget_Negative_Throws()
    {
        var user = new User("a@b.com", "Name", "hash");

        Assert.Throws<ValidationException>(() => user.SetMaintenanceBudget(-1m, null));
        Assert.Throws<ValidationException>(() => user.SetMaintenanceBudget(null, -1m));
    }

    [Fact]
    public void Anonymize_ShouldClearBudgets()
    {
        var user = new User("a@b.com", "Name", "hash");
        user.SetMaintenanceBudget(100m, 1000m);

        user.Anonymize();

        Assert.Null(user.MonthlyMaintenanceBudget);
        Assert.Null(user.AnnualMaintenanceBudget);
    }
}
