using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// An outgoing email. <see cref="HtmlBody"/> is optional (the message is then
    /// plain text only); <see cref="UnsubscribeUrl"/> is the RFC 8058 one-click
    /// endpoint advertised in the <c>List-Unsubscribe</c> headers, which mailbox
    /// providers require for bulk-style mail such as the reminder digest.
    /// </summary>
    public sealed record EmailMessage(
        string To,
        string Subject,
        string TextBody,
        string? HtmlBody = null,
        string? UnsubscribeUrl = null);

    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message);
    }

    public static class EmailSenderExtensions
    {
        /// <summary>Plain-text shortcut for callers with no HTML or unsubscribe link.</summary>
        public static Task SendAsync(this IEmailSender sender, string toEmail, string subject, string body) =>
            sender.SendAsync(new EmailMessage(toEmail, subject, body));
    }
}
