using Bikontrol.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Bikontrol.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            if (context.Response.HasStarted)
            {
                // La respuesta ya empezó a enviarse: no se puede reescribir el
                // status/body sin romper el stream, así que solo se registra.
                logger.LogWarning(exception, "Exception after response started: {Message}", exception.Message);
                return;
            }

            HttpStatusCode statusCode;
            string message;

            switch (exception)
            {
                case AuthException authEx:
                    statusCode = (HttpStatusCode)authEx.StatusCode;
                    message = authEx.Message;
                    break;

                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;

                case ForbiddenAccessException:
                    statusCode = HttpStatusCode.Forbidden;
                    message = exception.Message;
                    break;

                case ValidationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "Los datos cambiaron mientras los editabas. Recarga e inténtalo de nuevo.";
                    logger.LogWarning(exception, "Concurrency conflict: {Message}", exception.Message);
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "Error inesperado en el servidor.";
                    logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new { error = message };
            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}
