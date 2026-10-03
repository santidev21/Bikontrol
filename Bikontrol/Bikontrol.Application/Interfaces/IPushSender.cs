using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Delivers a Web Push notification to a browser subscription. Abstracted so
    /// the reminder flow is testable without a real push service.
    /// </summary>
    public interface IPushSender
    {
        /// <summary>
        /// Sends the payload. Returns true when delivered, false when the
        /// subscription is gone/expired (the caller should drop it).
        /// </summary>
        Task<bool> SendAsync(PushSubscription subscription, PushPayload payload);
    }

    public class PushPayload
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Url { get; set; } = "/dashboard";
    }
}
