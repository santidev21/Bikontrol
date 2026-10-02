using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Bikontrol.Tests.Integration.Infrastructure;

/// <summary>
/// Test-only startup filter that lets a request override its client IP through
/// the <c>X-Test-Client-Ip</c> header. <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>
/// leaves <c>Connection.RemoteIpAddress</c> null, so every request would share
/// the "unknown" rate-limit partition; this makes each test able to use its own
/// partition. It runs before the application pipeline (and therefore before
/// <c>UseRateLimiter</c>), and is completely inert when the header is absent.
/// </summary>
public sealed class ClientIpStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var value)
                    && IPAddress.TryParse(value.ToString(), out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}
