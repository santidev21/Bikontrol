using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace Bikontrol.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        public const string RefreshTokenCookieName = "bikontrol_rt";

        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return WithRefreshTokenCookie(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var result = await _authService.LoginAsync(dto);
            return WithRefreshTokenCookie(result);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _authService.GoogleLoginAsync(request);
            return WithRefreshTokenCookie(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            // Browser flow: the refresh token rides the HttpOnly cookie. Scripts
            // and tooling can still pass it in the body as a fallback.
            var refreshToken = ReadRefreshTokenCookie() ?? request.RefreshToken;
            var result = await _authService.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
            return WithRefreshTokenCookie(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Contraseña actualizada. Ya puedes iniciar sesión." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            await _authService.ConfirmEmailAsync(request);
            return Ok(new { message = "Correo confirmado. Ya puedes iniciar sesión." });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            await _authService.ResendConfirmationAsync(request);
            return Ok(new { message = "Si tu cuenta existe y no está confirmada, te enviamos un nuevo enlace." });
        }

        [HttpPost("demo")]
        public async Task<IActionResult> DemoLogin()
        {
            var result = await _authService.DemoLoginAsync();
            return WithRefreshTokenCookie(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            // Best-effort revocation of the presented token, then the cookie is
            // cleared regardless: script-side JS can neither read nor delete an
            // HttpOnly cookie, so the server has to do it.
            await _authService.LogoutAsync(ReadRefreshTokenCookie() ?? request?.RefreshToken);
            ClearRefreshTokenCookie();
            return Ok(new { message = "Sesión cerrada." });
        }

        /// <summary>
        /// Moves the rotated refresh token out of the JSON body and into an
        /// HttpOnly cookie scoped to the auth endpoints: script-injected JS can
        /// never read it, and it is only ever sent to /api/auth/*.
        /// </summary>
        private IActionResult WithRefreshTokenCookie(LoginResponse result)
        {
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = string.Empty;
            }

            return Ok(result);
        }

        // RegisterResponse is not a LoginResponse in C# (only in the TS types);
        // the register flow gets the same treatment.
        private IActionResult WithRefreshTokenCookie(RegisterResponse result)
        {
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = string.Empty;
            }

            return Ok(result);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var days = int.TryParse(_configuration["Jwt:RefreshExpireDays"], out var parsed) && parsed > 0 ? parsed : 30;
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                // Unconditional Secure: browsers treat http://localhost as a
                // trustworthy origin and still store Secure cookies there, so
                // local development (plain HTTP) keeps working.
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                MaxAge = TimeSpan.FromDays(days)
            });
        }

        private string? ReadRefreshTokenCookie() => Request.Cookies[RefreshTokenCookieName];

        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                Path = "/api/auth",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }
    }
}
