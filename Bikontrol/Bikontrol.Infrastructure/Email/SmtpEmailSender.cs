using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure.Resilience;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using AuthenticationException = MailKit.Security.AuthenticationException;
using System;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly IHostEnvironment _environment;

        public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger, IHostEnvironment environment)
        {
            _options = options.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendAsync(EmailMessage email)
        {
            var toEmail = email.To;
            var subject = email.Subject;
            var body = email.TextBody;
            if (!_options.IsConfigured)
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

            var username = _options.Username!;
            var password = _options.Password!;
            var smtpHost = _options.Host!;
            var port = _options.Port;
            var useSsl = _options.EnableSsl;
            var fromEmail = string.IsNullOrWhiteSpace(_options.FromEmail) ? username : _options.FromEmail;
            var fromName = _options.FromName;

            var message = BuildMessage(email, fromEmail, fromName);

            // A hung SMTP server must not pin a request (register/forgot-password
            // await this) for MailKit's two-minute default.
            var timeoutMs = _options.TimeoutSeconds * 1000;

            await TransientRetry.ExecuteAsync(async ct =>
            {
                using var client = new SmtpClient { Timeout = timeoutMs };
                await client.ConnectAsync(
                    smtpHost,
                    port,
                    useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                    ct);

                await client.AuthenticateAsync(username, password, ct);

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                return true;
            }, IsTransient);
        }

        /// <summary>
        /// Builds the MIME message: text and HTML alternatives plus, for bulk-style
        /// mail, the RFC 2369 / RFC 8058 one-click unsubscribe headers that Gmail
        /// and Outlook use to show their own "Unsubscribe" button.
        /// </summary>
        public static MimeMessage BuildMessage(EmailMessage email, string fromEmail, string fromName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = email.Subject;
            message.Body = new BodyBuilder { TextBody = email.TextBody, HtmlBody = email.HtmlBody }.ToMessageBody();

            if (!string.IsNullOrWhiteSpace(email.UnsubscribeUrl))
            {
                message.Headers.Add("List-Unsubscribe", $"<{email.UnsubscribeUrl}>");
                message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
            }

            return message;
        }

        /// <summary>Network hiccups, timeouts and 4xx SMTP replies are worth another try; auth or address errors are not.</summary>
        public static bool IsTransient(Exception exception) => exception switch
        {
            SmtpCommandException command => (int)command.StatusCode is >= 400 and < 500,
            AuthenticationException => false,
            SmtpProtocolException => true,
            System.IO.IOException => true,
            System.Net.Sockets.SocketException => true,
            TimeoutException => true,
            OperationCanceledException => true,
            _ => false
        };
    }
}
