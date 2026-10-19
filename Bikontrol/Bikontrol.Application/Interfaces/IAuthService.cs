using Bikontrol.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest dto);
        Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
        Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task ConfirmEmailAsync(ConfirmEmailRequest request);
        Task ResendConfirmationAsync(ResendConfirmationRequest request);
        Task<LoginResponse> DemoLoginAsync();

        /// <summary>
        /// Best-effort revocation of the presented refresh token (logout). A
        /// missing or unknown token is a no-op: logout never fails the user.
        /// </summary>
        Task LogoutAsync(string? refreshToken);
    }
}
