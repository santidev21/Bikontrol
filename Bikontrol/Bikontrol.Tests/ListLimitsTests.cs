using Bikontrol.Application.Services;

namespace Bikontrol.Tests;

public class ListLimitsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Clamp_WhenMissingOrInvalid_ShouldMeanNoLimit(int? requested)
    {
        Assert.Null(ListLimits.Clamp(requested));
    }

    [Fact]
    public void Clamp_WhenWithinTheCeiling_ShouldKeepTheValue()
    {
        Assert.Equal(100, ListLimits.Clamp(100));
    }

    [Fact]
    public void Clamp_WhenOverTheCeiling_ShouldClamp()
    {
        Assert.Equal(ListLimits.MaxLimit, ListLimits.Clamp(999_999));
    }
}
