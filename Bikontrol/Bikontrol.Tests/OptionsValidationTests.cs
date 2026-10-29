using Bikontrol.Application.Options;
using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Tests;

public class OptionsValidationTests
{
    private static List<ValidationResult> Validate(object options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    private static JwtOptions ValidJwt() => new()
    {
        Key = new string('k', 32),
        Issuer = "Bikontrol",
        Audience = "Bikontrol.Tests"
    };

    [Fact]
    public void Jwt_WithAllRequiredValues_ShouldBeValid() => Assert.Empty(Validate(ValidJwt()));

    [Fact]
    public void Jwt_WithAShortKey_ShouldBeRejected()
    {
        var options = ValidJwt();
        options.Key = "too-short";

        Assert.Contains(Validate(options), r => r.ErrorMessage!.Contains("at least 32"));
    }

    [Theory]
    [InlineData(nameof(JwtOptions.Issuer))]
    [InlineData(nameof(JwtOptions.Audience))]
    public void Jwt_WithoutIssuerOrAudience_ShouldBeRejected(string property)
    {
        var options = ValidJwt();
        typeof(JwtOptions).GetProperty(property)!.SetValue(options, "");

        Assert.NotEmpty(Validate(options));
    }

    [Fact]
    public void Jwt_WithAnOutOfRangeLifetime_ShouldBeRejected()
    {
        var options = ValidJwt();
        options.RefreshExpireDays = 0;

        Assert.NotEmpty(Validate(options));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void Reminders_WithAnInvalidHour_ShouldBeRejected(int hour) =>
        Assert.NotEmpty(Validate(new ReminderOptions { DailyHourUtc = hour }));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Retention_WithNonPositiveDays_ShouldBeRejected(int days)
    {
        Assert.NotEmpty(Validate(new RetentionOptions { RefreshTokenDays = days }));
        Assert.NotEmpty(Validate(new RetentionOptions { ReminderLogDays = days }));
    }

    [Fact]
    public void Retention_DefaultsKeepAuditLogsForever()
    {
        var options = new RetentionOptions();

        Assert.Empty(Validate(options));
        Assert.Equal(0, options.AuditLogDays);
    }

    [Fact]
    public void WebPush_WithOnlyOneKey_ShouldBeRejected()
    {
        Assert.NotEmpty(Validate(new WebPushOptions { PublicKey = "pub" }));
        Assert.NotEmpty(Validate(new WebPushOptions { PrivateKey = "priv" }));
        Assert.Empty(Validate(new WebPushOptions { PublicKey = "pub", PrivateKey = "priv" }));
        Assert.Empty(Validate(new WebPushOptions()));
    }

    [Fact]
    public void Smtp_IsConfigured_RequiresHostAndCredentialsWithoutPlaceholders()
    {
        Assert.False(new SmtpOptions().IsConfigured);
        Assert.False(new SmtpOptions { Host = "CHANGE_ME", Username = "u", Password = "p" }.IsConfigured);
        Assert.True(new SmtpOptions { Host = "smtp.example.com", Username = "u", Password = "p" }.IsConfigured);
    }

    [Fact]
    public void Smtp_WithAnInvalidPort_ShouldBeRejected() =>
        Assert.NotEmpty(Validate(new SmtpOptions { Port = 70000 }));
}
