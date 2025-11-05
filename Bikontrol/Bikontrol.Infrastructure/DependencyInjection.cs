using Bikontrol.Application.Interfaces;
using Bikontrol.Infrastructure.Authentication;
using Bikontrol.Infrastructure.Mapping;
using Bikontrol.Infrastructure.Services;
using Bikontrol.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
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
            services.AddScoped<JwtTokenGenerator>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IMotorcycleService, MotorcycleService>();
            services.AddAutoMapper(typeof(MappingProfile));
            return services;
        }
    }
}
