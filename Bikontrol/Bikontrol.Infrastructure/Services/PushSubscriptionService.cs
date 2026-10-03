using Bikontrol.Application.DTOs.Reminders;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Domain.Entities;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Infrastructure.Services
{
    public class PushSubscriptionService : IPushSubscriptionService
    {
        private readonly IPushSubscriptionRepository _repository;
        private readonly ICurrentUserService _current;

        public PushSubscriptionService(IPushSubscriptionRepository repository, ICurrentUserService current)
        {
            _repository = repository;
            _current = current;
        }

        public async Task RegisterAsync(RegisterPushSubscriptionRequest request)
        {
            EnsureCanWrite();
            if (string.IsNullOrWhiteSpace(request.Endpoint)
                || string.IsNullOrWhiteSpace(request.Keys?.P256dh)
                || string.IsNullOrWhiteSpace(request.Keys?.Auth))
                throw new ValidationException("La suscripción de notificaciones está incompleta.");

            await _repository.UpsertAsync(new PushSubscription
            {
                UserId = _current.UserId,
                Endpoint = request.Endpoint,
                P256dh = request.Keys.P256dh,
                Auth = request.Keys.Auth
            });
            await _repository.SaveChangesAsync();
        }

        public async Task UnregisterAsync(string endpoint)
        {
            EnsureCanWrite();
            if (string.IsNullOrWhiteSpace(endpoint))
                return;

            await _repository.RemoveByEndpointAsync(endpoint);
            await _repository.SaveChangesAsync();
        }

        private void EnsureCanWrite()
        {
            if (_current.IsDemo)
                throw new ForbiddenAccessException("El usuario demo solo puede visualizar información.");
        }
    }
}
