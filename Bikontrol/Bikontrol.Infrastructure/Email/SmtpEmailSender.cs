using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly IHostEnvironment _environment;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger, IHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"] ?? string.Empty;
            var password = _configuration["Smtp:Password"] ?? string.Empty;

            var hostConfigured = !string.IsNullOrWhiteSpace(host) && !host.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
            var credentialsConfigured = !string.IsNullOrWhiteSpace(username)
                && !username.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(password)
                && !password.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

            if (!hostConfigured || !credentialsConfigured)
            {
                if (_environment.IsDevelopment())
                {
                    // No SMTP configured in local dev: log the message so the
                    // reset link is still usable without an email provider.
                    _logger.LogWarning(
                        "[SMTP] not configured — email to {Email} was not sent. Subject: {Subject}. Body: {Body}",
                        toEmail, subject, body);
                }
                else
                {
                    // Never log the body (it may contain a live reset token).
                    _logger.LogWarning(
                        "[SMTP] not configured — email to {Email} was not sent. Subject: {Subject}.",
                        toEmail, subject);
                }
                return;
            }

            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var useSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username;
            var fromName = _configuration["Smtp:FromName"] ?? "Bikontrol";
            var smtpHost = host ?? string.Empty;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpHost,
                port,
                useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            await client.AuthenticateAsync(username, password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
