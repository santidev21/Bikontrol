using System.Text.Json;

namespace Bikontrol.API.Middleware
{
    /// <summary>
    /// The one error body every non-2xx answer uses: RFC 9457 problem details
    /// plus a stable machine <c>code</c>. <c>error</c> repeats the message for
    /// the existing client contract (<c>{ error, code }</c>).
    /// </summary>
    public static class ErrorResponse
    {
        public const string ContentType = "application/problem+json";

        public static async Task WriteAsync(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = ContentType;

            var body = new Dictionary<string, object?>
            {
                ["type"] = $"urn:bikontrol:error:{code}",
                ["title"] = message,
                ["status"] = status,
                ["detail"] = message,
                ["code"] = code,
                ["error"] = message,
                ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body), context.RequestAborted);
        }

        /// <summary>Default code and message for a bare status (no controller body).</summary>
        public static (string Code, string Message)? ForStatus(int status) => status switch
        {
            StatusCodes.Status401Unauthorized => ("unauthorized", "Tu sesión no es válida. Inicia sesión de nuevo."),
            StatusCodes.Status403Forbidden => ("forbidden", "No tienes permiso para realizar esta acción."),
            StatusCodes.Status404NotFound => ("not_found", "El recurso solicitado no existe."),
            StatusCodes.Status413PayloadTooLarge => ("payload_too_large", "El archivo o los datos enviados son demasiado grandes."),
            StatusCodes.Status429TooManyRequests => ("rate_limited", "Demasiadas solicitudes. Espera un momento e inténtalo de nuevo."),
            _ => null
        };
    }
}
