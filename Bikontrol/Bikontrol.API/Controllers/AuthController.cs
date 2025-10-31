using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
    }
}
