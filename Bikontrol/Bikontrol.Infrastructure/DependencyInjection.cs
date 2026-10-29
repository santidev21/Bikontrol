using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Infrastructure.Email;
using Bikontrol.Infrastructure.Authentication;
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
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            services.AddScoped<IMaintenanceBookPdfRenderer, Reports.QuestPdfMaintenanceBookRenderer>();
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

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPushSender, Notifications.WebPushSender>();
            return services;
        }

        /// <summary>
        /// Registers the daily reminder and retention jobs. Split from
        /// <see cref="AddInfrastructure"/> so a deployment can run them in a
        /// dedicated worker container (the API sets <c>Jobs:Enabled=false</c>).
        /// </summary>
        public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            services.AddHostedService<BackgroundServices.ReminderBackgroundService>();
            services.AddHostedService<BackgroundServices.RetentionBackgroundService>();
            return services;
        }
    }
}
