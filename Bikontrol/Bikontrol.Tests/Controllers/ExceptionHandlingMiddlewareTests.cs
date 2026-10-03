using System.Text.Json;
using Bikontrol.API.Middleware;
using Bikontrol.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bikontrol.Tests.Controllers;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAlwaysEchoRequestIdHeader()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("trace-123", context.Response.Headers["X-Request-Id"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthException_ShouldMapStatusCodeAndMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new AuthException("Bloqueada", 429),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(429, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Bloqueada", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpected_ShouldReturn500WithGenericMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        // The message must not leak internals.
        Assert.Equal("Error inesperado en el servidor.", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseAlreadyStarted_ShouldNotRewriteIt()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(
            new StartedResponseFeature());
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("too late"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        // Status stays the default because rewriting a started response is illegal.
        Assert.Equal(200, context.Response.StatusCode);
    }

    private sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
