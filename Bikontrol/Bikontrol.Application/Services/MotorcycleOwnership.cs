using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Application.Services
{
    /// <summary>Single place that decides whether a user may touch a motorcycle.</summary>
    internal static class MotorcycleOwnership
    {
        /// <summary>Returns the motorcycle, or throws when it is missing or owned by someone else.</summary>
        public static async Task<Motorcycle> EnsureOwnedAsync(IMotorcycleRepository motorcycles, Guid userId, Guid motorcycleId)
        {
            var motorcycle = await motorcycles.GetByIdAsync(motorcycleId);
            if (motorcycle is null) throw new NotFoundException("Motocicleta no encontrada.", "motorcycle_not_found");
            if (motorcycle.UserId != userId)
                throw new ForbiddenAccessException("No tienes permisos para esta motocicleta.", "motorcycle_no_access");
            return motorcycle;
        }

        /// <summary>Throws when any of the motorcycles is not owned by the user.</summary>
        public static async Task EnsureAllOwnedAsync(IMotorcycleRepository motorcycles, Guid userId, IReadOnlyCollection<Guid> motorcycleIds)
        {
            var owned = (await motorcycles.GetByUserIdAsync(userId))
                .Select(m => m.Id)
                .ToHashSet();

            if (motorcycleIds.Any(id => !owned.Contains(id)))
                throw new ForbiddenAccessException("No tienes permisos para ver estas motocicletas.", "motorcycles_no_access");
        }
    }
}
