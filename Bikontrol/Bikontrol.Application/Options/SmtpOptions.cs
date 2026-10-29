using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>
    /// Outbound email (<c>Smtp</c> section). Optional: without a host and
    /// credentials the sender logs instead of sending (local dev, CI).
    /// </summary>
    public class SmtpOptions
    {
        public const string Section = "Smtp";

        public string? Host { get; set; }

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? FromEmail { get; set; }

        public string FromName { get; set; } = "Bikontrol";

        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 15;

        private static bool IsSet(string? value) =>
            !string.IsNullOrWhiteSpace(value) && !value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

        /// <summary>True when there is enough configuration to actually send mail.</summary>
        public bool IsConfigured => IsSet(Host) && IsSet(Username) && IsSet(Password);
    }
}
