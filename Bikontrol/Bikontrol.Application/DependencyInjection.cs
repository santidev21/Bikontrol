using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bikontrol.Application
{
    public static class DependencyInjection
    {
        /// <summary>Registers the use-case services (business logic).</summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMotorcycleService, MotorcycleService>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            services.AddScoped<IFuelUpService, FuelUpService>();
            services.AddScoped<IKmHistoryService, KmHistoryService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddSingleton<IUnsubscribeTokenService, UnsubscribeTokenService>();
            services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
            services.AddScoped<IMaintenanceBookService, MaintenanceBookService>();
            services.AddScoped<IMaintenanceAttachmentService, MaintenanceAttachmentService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IRetentionService, RetentionService>();
            return services;
        }
    }
}
