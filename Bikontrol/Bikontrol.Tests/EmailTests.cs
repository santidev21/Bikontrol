using Bikontrol.Application.Email;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Bikontrol.Application.Services;
using Bikontrol.Infrastructure.Email;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bikontrol.Tests;

public class EmailTests
{
    private static UnsubscribeTokenService TokenService(char keyChar = 'a') =>
        new(Options.Create(new JwtOptions { Key = new string(keyChar, 40) }));

    [Fact]
    public void UnsubscribeToken_RoundTrips()
    {
        var service = TokenService();
        var userId = Guid.NewGuid();

        Assert.True(service.TryValidate(service.Create(userId), out var parsed));
        Assert.Equal(userId, parsed);
    }

    [Fact]
    public void UnsubscribeToken_IsRejectedWhenTamperedOrSignedWithAnotherKey()
    {
        var service = TokenService();
        var token = service.Create(Guid.NewGuid());
        var other = TokenService('f').Create(Guid.NewGuid());
        var swapped = token.Split('.')[0] + "." + other.Split('.')[1];

        Assert.False(service.TryValidate(swapped, out _));
        Assert.False(service.TryValidate(other, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-dot")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    public void UnsubscribeToken_RejectsGarbage(string? token)
    {
        Assert.False(TokenService().TryValidate(token, out var userId));
        Assert.Equal(Guid.Empty, userId);
    }

    [Fact]
    public void Template_EncodesEveryDynamicValue()
    {
        var html = EmailTemplate.Render(
            heading: "<script>alert(1)</script>",
            paragraphs: new[] { "Hola <b>Ana</b> & co" },
            bullets: new[] { "<img src=x onerror=y>" },
            buttonLabel: "Ir",
            buttonUrl: "https://app.test/x?a=1&b=\"2\"",
            footerLine: "<i>pie</i>",
            footerLinkLabel: "Baja",
            footerLinkUrl: "https://app.test/u?t=1&x=2");

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("<b>Ana</b>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("a=1&amp;b=&quot;2&quot;", html);
        Assert.Contains("Baja</a>", html);
    }

    [Fact]
    public void Message_IsMultipartAndCarriesOneClickUnsubscribeHeaders()
    {
        var email = new EmailMessage("rider@bikontrol.test", "Asunto", "texto", "<p>html</p>", "https://app.test/api/reminders/email/unsubscribe?token=t");

        var message = SmtpEmailSender.BuildMessage(email, "no-reply@bikontrol.test", "Bikontrol");

        Assert.Equal("texto", message.TextBody);
        Assert.Equal("<p>html</p>", message.HtmlBody);
        Assert.Equal("<https://app.test/api/reminders/email/unsubscribe?token=t>", message.Headers["List-Unsubscribe"]);
        Assert.Equal("List-Unsubscribe=One-Click", message.Headers["List-Unsubscribe-Post"]);
    }

    [Fact]
    public void Message_WithoutUnsubscribeUrl_HasNoListHeaders()
    {
        var message = SmtpEmailSender.BuildMessage(new EmailMessage("a@b.test", "s", "t"), "x@y.test", "Bikontrol");

        Assert.Null(message.Headers["List-Unsubscribe"]);
        Assert.Null(message.HtmlBody);
        Assert.Equal("t", message.TextBody);
    }
}
