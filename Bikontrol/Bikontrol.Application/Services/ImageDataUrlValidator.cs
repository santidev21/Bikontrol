using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Validates image data URLs (JPEG/PNG/WebP) accepted from the client. Shared
    /// by motorcycle images and maintenance-record attachments so the rules — and
    /// their limits — live in one place.
    /// </summary>
    public static class ImageDataUrlValidator
    {
        /// <summary>~1 MB decoded image.</summary>
        public const int MaxDecodedBytes = 1_048_576;

        private static readonly string[] AllowedPrefixes =
        {
            "data:image/jpeg;base64,",
            "data:image/png;base64,",
            "data:image/webp;base64,"
        };

        /// <summary>
        /// Throws a <see cref="ValidationException"/> when the value is not an
        /// accepted image data URL. Empty/null is treated as "no image" and is
        /// allowed only when <paramref name="required"/> is false.
        /// </summary>
        public static void Validate(string? dataUrl, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(dataUrl) || dataUrl == "default.png")
            {
                if (required)
                    throw new ValidationException("La imagen es obligatoria.");
                return;
            }

            var prefix = AllowedPrefixes.FirstOrDefault(p => dataUrl.StartsWith(p, StringComparison.Ordinal));
            if (prefix is null)
                throw new ValidationException("La imagen debe ser un data URL JPEG, PNG o WebP.");

            var payload = dataUrl[prefix.Length..];
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new ValidationException("La imagen no es válida.");
            }

            if (bytes.Length == 0 || bytes.Length > MaxDecodedBytes)
                throw new ValidationException("La imagen es demasiado grande.");
        }

        /// <summary>Maps an accepted data URL prefix to its MIME type.</summary>
        public static string ContentTypeOf(string dataUrl)
        {
            if (dataUrl.StartsWith("data:image/png", StringComparison.Ordinal)) return "image/png";
            if (dataUrl.StartsWith("data:image/webp", StringComparison.Ordinal)) return "image/webp";
            return "image/jpeg";
        }
    }
}
