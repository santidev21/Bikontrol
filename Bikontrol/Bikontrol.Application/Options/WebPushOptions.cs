using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>VAPID Web Push (<c>WebPush</c> section). Optional, but the key pair is all-or-nothing.</summary>
    public class WebPushOptions : IValidatableObject
    {
        public const string Section = "WebPush";

        public string? PublicKey { get; set; }

        public string? PrivateKey { get; set; }

        public string Subject { get; set; } = "mailto:contacto.santidev21@gmail.com";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(PublicKey) != string.IsNullOrWhiteSpace(PrivateKey))
                yield return new ValidationResult("WebPush:PublicKey and WebPush:PrivateKey must be set together.");
        }
    }
}
