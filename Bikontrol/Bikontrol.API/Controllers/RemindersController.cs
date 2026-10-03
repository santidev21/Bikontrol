using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IConfiguration _configuration;

        public RemindersController(
            IReminderService reminderService,
            IPushSubscriptionService pushSubscriptionService,
            IConfiguration configuration)
        {
            _reminderService = reminderService;
            _pushSubscriptionService = pushSubscriptionService;
            _configuration = configuration;
        }

        /// <summary>Maintenance that is due or overdue, worst first.</summary>
        [HttpGet("due")]
        public async Task<IActionResult> GetDue()
        {
            var result = await _reminderService.GetDueForCurrentUserAsync();
            return Ok(result);
        }

        /// <summary>The VAPID public key the browser needs to subscribe (null when push is off).</summary>
        [HttpGet("push/vapid-public-key")]
        [AllowAnonymous]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _configuration["WebPush:PublicKey"];
            return Ok(new { publicKey = string.IsNullOrWhiteSpace(publicKey) ? null : publicKey });
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
