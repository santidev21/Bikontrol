using Bikontrol.Domain.Entities;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>Issues the short-lived access tokens.</summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>Lifetime of the tokens it issues, in seconds.</summary>
        int ExpiresInSeconds { get; }

        string GenerateToken(Guid userId, string email, string fullName, string role = UserRole.User);
    }
}
