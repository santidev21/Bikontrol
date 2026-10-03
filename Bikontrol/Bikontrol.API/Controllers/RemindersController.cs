using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.API.Controllers
{
    /// <summary>
    /// Maintenance reminders for the current user. The daily background job
    /// generates them; this endpoint surfaces what is due right now.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _reminderService;

        public RemindersController(IReminderService reminderService)
        {
            _reminderService = reminderService;
        }

        /// <summary>Maintenance that is due or overdue, worst first.</summary>
        [HttpGet("due")]
        public async Task<IActionResult> GetDue()
        {
            var result = await _reminderService.GetDueForCurrentUserAsync();
            return Ok(result);
        }
    }
}
