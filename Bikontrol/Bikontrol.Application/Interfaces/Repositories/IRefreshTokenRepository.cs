using Bikontrol.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task AddAsync(RefreshToken refreshToken);

        /// <summary>
        /// Revoca todos los refresh tokens activos del usuario (p. ej. al cambiar
        /// o restablecer la contraseña). Devuelve cuántos fueron revocados.
        /// No persiste por sí sola: se confirma con <see cref="SaveChangesAsync"/>.
        /// </summary>
        Task<int> RevokeAllForUserAsync(Guid userId);

        /// <summary>
        /// Bulk-deletes tokens that expired or were revoked before the cutoff
        /// (retention purge; bypasses the change tracker and the audit trail).
        /// Returns how many rows were deleted.
        /// </summary>
        Task<int> DeleteInactiveBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

        Task SaveChangesAsync();
    }
}
