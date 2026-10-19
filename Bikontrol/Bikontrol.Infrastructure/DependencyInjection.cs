using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<JwtTokenGenerator>();
            services.AddScoped<IPasswordHasher<User>>(sp =>
            {
                // OWASP's current guidance for PBKDF2-HMAC-SHA256 is 600k
                // iterations (Identity's v3 default is 100k). The exact value
                // is a CPU/latency trade-off, so it stays overridable; hashes
                // minted with older settings keep verifying because the
                // iteration count is embedded per hash.
                var configuration = sp.GetRequiredService<IConfiguration>();
                var raw = configuration["PasswordHashing:Iterations"];
                var iterations = int.TryParse(raw, out var parsed) && parsed >= ConfigurablePasswordHasher<User>.MinimumIterations
                    ? parsed
                    : 600_000;
                return new ConfigurablePasswordHasher<User>(iterations);
            });
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMotorcycleService, MotorcycleService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            services.AddScoped<IFuelUpService, FuelUpService>();
            services.AddScoped<IKmHistoryService, KmHistoryService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
            services.AddScoped<IMaintenanceBookService, MaintenanceBookService>();
            services.AddScoped<IMaintenanceAttachmentService, MaintenanceAttachmentService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IRetentionService, RetentionService>();
            services.AddScoped<IPushSender, Notifications.WebPushSender>();
            services.AddHostedService<BackgroundServices.ReminderBackgroundService>();
            services.AddHostedService<BackgroundServices.RetentionBackgroundService>();
            return services;
        }
    }
}
