using System.Text.Json;
using Bikontrol.Application.Interfaces;
using Bikontrol.Infrastructure.Resilience;
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
        // One shared client: new WebPushClient() builds an HttpClient (and a
        // socket pool) per notification, with no timeout.
        private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

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

            var client = new WebPushClient(SharedHttpClient);
            try
            {
                await TransientRetry.ExecuteAsync(async ct =>
                {
                    await client.SendNotificationAsync(pushSubscription, body, vapid, ct);
                    return true;
                }, IsTransient);
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

        /// <summary>429/5xx from the push service and network failures are retried; 4xx (bad subscription/VAPID) are not.</summary>
        public static bool IsTransient(Exception exception) => exception switch
        {
            WebPushException ex => (int)ex.StatusCode is 429 or >= 500,
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false
        };
    }
}
