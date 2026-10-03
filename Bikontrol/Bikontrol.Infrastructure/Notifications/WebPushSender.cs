using System.Text.Json;
using Bikontrol.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;
using PushSubscriptionEntity = Bikontrol.Domain.Entities.PushSubscription;

namespace Bikontrol.Infrastructure.Notifications
{
    /// <summary>
    /// VAPID Web Push delivery. Inert unless <c>WebPush:PublicKey</c> and
    /// <c>WebPush:PrivateKey</c> are configured, so local dev and CI never make
    /// outbound push calls.
    /// </summary>
    public class WebPushSender : IPushSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebPushSender> _logger;

        public WebPushSender(IConfiguration configuration, ILogger<WebPushSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["WebPush:PublicKey"])
            && !string.IsNullOrWhiteSpace(_configuration["WebPush:PrivateKey"]);

        public async Task<bool> SendAsync(PushSubscriptionEntity subscription, PushPayload payload)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[WebPush] not configured — notification to a subscription was skipped.");
                // Not configured is not "gone": keep the subscription.
                return true;
            }

            var subject = _configuration["WebPush:Subject"] ?? "mailto:contacto.santidev21@gmail.com";
            var vapid = new VapidDetails(
                subject,
                _configuration["WebPush:PublicKey"]!,
                _configuration["WebPush:PrivateKey"]!);

            var pushSubscription = new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
            // Payload shape expected by Angular's service worker (SwPush): the
            // service worker turns `notification` into a Notification and uses
            // `data.onActionClick` to open the app on click.
            var body = JsonSerializer.Serialize(new
            {
                notification = new
                {
                    title = payload.Title,
                    body = payload.Body,
                    data = new
                    {
                        url = payload.Url,
                        onActionClick = new
                        {
                            @default = new { operation = "openWindow", url = payload.Url }
                        }
                    }
                }
            });

            var client = new WebPushClient();
            try
            {
                await client.SendNotificationAsync(pushSubscription, body, vapid);
                return true;
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound
                || ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // 404/410: the subscription is dead. Ask the caller to drop it.
                _logger.LogInformation("[WebPush] subscription gone ({Status}); removing it.", ex.StatusCode);
                return false;
            }
        }
    }
}
