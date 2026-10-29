using Bikontrol.API.Middleware;
using Bikontrol.Application.Options;

namespace Bikontrol.Tests;

public class ErrorResponseTests
{
    [Theory]
    [InlineData(401, "unauthorized")]
    [InlineData(403, "forbidden")]
    [InlineData(404, "not_found")]
    [InlineData(413, "payload_too_large")]
    [InlineData(429, "rate_limited")]
    public void ForStatus_ShouldMapBareStatusesToStableCodes(int status, string code)
    {
        var mapped = ErrorResponse.ForStatus(status);

        Assert.NotNull(mapped);
        Assert.Equal(code, mapped!.Value.Code);
        Assert.False(string.IsNullOrWhiteSpace(mapped.Value.Message));
    }

    [Fact]
    public void ForStatus_ShouldLeaveOtherStatusesAlone()
    {
        Assert.Null(ErrorResponse.ForStatus(500));
    }

    [Fact]
    public void JwtOptions_ShouldDefaultToNoPreviousKeysAndACachedStamp()
    {
        var options = new JwtOptions();

        Assert.Empty(options.PreviousKeys);
        Assert.True(options.SecurityStampCacheSeconds > 0);
    }
}
