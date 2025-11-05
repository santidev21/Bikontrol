using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Domain.Entities;
using Bikontrol.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mapping
            CreateMap<RegisterRequest, User>();
            CreateMap<User, RegisterResponse>();
            CreateMap<User, LoginResponse>();

            // Motorcycle mapping
            CreateMap<CreateMotorcycleDTO, Motorcycle>();
            CreateMap<Motorcycle, MotorcycleDTO>();
            CreateMap<UpdateMotorcycleDTO, Motorcycle>();
        }
    }
}
