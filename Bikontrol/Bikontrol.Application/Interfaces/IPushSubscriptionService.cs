using Bikontrol.Application.DTOs.Reminders;

namespace Bikontrol.Application.Interfaces
{
    public interface IPushSubscriptionService
    {
        /// <summary>Registers (or refreshes) the current user's browser subscription.</summary>
        Task RegisterAsync(RegisterPushSubscriptionRequest request);

        /// <summary>Removes a subscription for the current user by endpoint.</summary>
        Task UnregisterAsync(string endpoint);
    }
}
