using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>Issues the short-lived access tokens.</summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>Lifetime of the tokens it issues, in seconds.</summary>
        int ExpiresInSeconds { get; }

        /// <summary>Lifetime of the refresh tokens, in days.</summary>
        int RefreshExpireDays { get; }

        /// <param name="securityStamp">Embedded as the <c>sstamp</c> claim so a stamp rotation revokes the token.</param>
        string GenerateToken(Guid userId, string email, string fullName, string role = UserRole.User, Guid? securityStamp = null);
    }
}
