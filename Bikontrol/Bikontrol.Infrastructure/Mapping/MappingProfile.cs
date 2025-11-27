using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.DTOs.Maintenance;
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
            CreateMap<SaveMotorcycleDTO, Motorcycle>();
            CreateMap<Motorcycle, MotorcycleDTO>();

            // MaintenanceType mapping
            CreateMap<MaintenanceType, MaintenanceTypeDTO>()
                .ForMember(dest => dest.KmInterval, opt => opt.MapFrom(src => src.DefaultKmInterval))
                .ForMember(dest => dest.TimeIntervalWeeks, opt => opt.MapFrom(src => src.DefaultTimeIntervalWeeks));

            CreateMap<CreateMaintenanceTypeDTO, MaintenanceType>()
                .ForMember(dest => dest.DefaultKmInterval, opt => opt.MapFrom(src => src.KmInterval))
                .ForMember(dest => dest.DefaultTimeIntervalWeeks, opt => opt.MapFrom(src => src.TimeIntervalWeeks));

            CreateMap<UserMaintenanceType, MaintenanceTypeDTO>();
            CreateMap<CreateMaintenanceTypeDTO, UserMaintenanceType>();


        }
    }
}
