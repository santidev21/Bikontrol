using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bikontrol.Application.DTOs.Users;

namespace Bikontrol.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly IAccountService _accountService;

        public UsersController(IUserService userService, IAuditLogService auditLogService, IAccountService accountService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
            _accountService = accountService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var result = await _userService.GetMeAsync();
            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            var result = await _userService.UpdateProfileAsync(request);
            return Ok(result);
        }

        [HttpPost("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await _userService.ChangePasswordAsync(request);
            return Ok(new { message = "Contraseña actualizada." });
        }

        [HttpPut("me/reminders")]
        public async Task<IActionResult> UpdateReminders([FromBody] UpdateRemindersRequest request)
        {
            var result = await _userService.UpdateRemindersAsync(request);
            return Ok(result);
        }

        [HttpGet("me/activity")]
        public async Task<IActionResult> GetMyActivity([FromQuery] int limit = 50)
        {
            return Ok(await _auditLogService.GetMyActivityAsync(limit));
        }

        /// <summary>Downloads everything the current user owns as one document.</summary>
        [HttpGet("me/export")]
        public async Task<IActionResult> ExportMyData()
        {
            return Ok(await _accountService.ExportMyDataAsync());
        }
    }
}
