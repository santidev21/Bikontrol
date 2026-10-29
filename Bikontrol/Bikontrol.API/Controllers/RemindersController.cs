using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Bikontrol.API.Controllers
{
    /// <summary>
    /// Maintenance reminders for the current user. The daily background job
    /// generates and delivers them; these endpoints surface what is due and
    /// manage Web Push subscriptions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        private readonly IPushSubscriptionService _pushSubscriptionService;
        private readonly WebPushOptions _webPush;

        public RemindersController(
            IReminderService reminderService,
            IPushSubscriptionService pushSubscriptionService,
            IOptions<WebPushOptions> webPush)
        {
            _reminderService = reminderService;
            _pushSubscriptionService = pushSubscriptionService;
            _webPush = webPush.Value;
        }

        /// <summary>Maintenance that is due or overdue, worst first.</summary>
        [HttpGet("due")]
        public async Task<IActionResult> GetDue()
        {
            var result = await _reminderService.GetDueForCurrentUserAsync();
            return Ok(result);
        }

        /// <summary>
        /// One-click unsubscribe from reminder emails (RFC 8058). Anonymous by
        /// design: the signed token in the link identifies the user, so mail
        /// clients can POST here straight from the inbox. Idempotent.
        /// </summary>
        [HttpPost("email/unsubscribe")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> UnsubscribeEmail([FromQuery] string? token)
        {
            await _reminderService.UnsubscribeAsync(token);
            return NoContent();
        }

        /// <summary>The VAPID public key the browser needs to subscribe (null when push is off).</summary>
        [HttpGet("push/vapid-public-key")]
        [AllowAnonymous]
        public IActionResult GetVapidPublicKey()
        {
            return Ok(new { publicKey = string.IsNullOrWhiteSpace(_webPush.PublicKey) ? null : _webPush.PublicKey });
        }

        /// <summary>Registers (or refreshes) the current browser's push subscription.</summary>
        [HttpPost("push/subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] RegisterPushSubscriptionRequest request)
        {
            await _pushSubscriptionService.RegisterAsync(request);
            return Ok(new { message = "Notificaciones activadas." });
        }

        /// <summary>Removes this browser's push subscription.</summary>
        [HttpPost("push/unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnregisterPushSubscriptionRequest request)
        {
            await _pushSubscriptionService.UnregisterAsync(request.Endpoint);
            return Ok(new { message = "Notificaciones desactivadas." });
        }
    }
}
