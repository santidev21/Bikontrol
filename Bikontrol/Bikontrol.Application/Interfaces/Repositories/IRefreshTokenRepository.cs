using Bikontrol.Persistence.Entities;
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

        Task SaveChangesAsync();
    }
}
