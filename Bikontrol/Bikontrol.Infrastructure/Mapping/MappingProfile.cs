using AutoMapper;
using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;
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
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.ResetPasswordTokenHash, opt => opt.Ignore())
                .ForMember(dest => dest.ResetPasswordTokenExpires, opt => opt.Ignore())
                .ForMember(dest => dest.AuthProvider, opt => opt.Ignore())
                .ForMember(dest => dest.Motorcycles, opt => opt.Ignore());
            CreateMap<User, RegisterResponse>()
                .ForMember(dest => dest.Token, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresIn, opt => opt.Ignore());
            CreateMap<User, LoginResponse>()
                .ForMember(dest => dest.Token, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresIn, opt => opt.Ignore());
            CreateMap<User, ProfileDTO>();

            // Motorcycle mapping
            CreateMap<SaveMotorcycleDTO, Motorcycle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.KmHistory, opt => opt.Ignore())
                .ForMember(dest => dest.UserMaintenances, opt => opt.Ignore())
                .ForMember(dest => dest.MaintenanceRecords, opt => opt.Ignore());
            // Km sale de MotorcycleKmHistory, no del agregado Motorcycle:
            // se resuelve en MotorcycleService, no en el mapper.
            CreateMap<Motorcycle, MotorcycleDTO>()
                .ForMember(dest => dest.Km, opt => opt.Ignore());

            // MaintenanceType mapping
            CreateMap<Maintenance, MaintenanceDTO>()
                .ForMember(dest => dest.KmInterval, opt => opt.MapFrom(src => src.DefaultKmInterval))
                .ForMember(dest => dest.TimeIntervalWeeks, opt => opt.MapFrom(src => src.DefaultTimeIntervalWeeks))
                .ForMember(dest => dest.TrackingType, opt => opt.MapFrom(src => src.TrackingType))
                .ForMember(dest => dest.MotorcycleId, opt => opt.Ignore())
                .ForMember(dest => dest.BaseTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.IsSystem, opt => opt.Ignore());

            CreateMap<SaveMaintenanceDTO, Maintenance>()
                .ForMember(dest => dest.DefaultKmInterval, opt => opt.MapFrom(src => src.KmInterval))
                .ForMember(dest => dest.DefaultTimeIntervalWeeks, opt => opt.MapFrom(src => src.TimeIntervalWeeks))
                .ForMember(dest => dest.TrackingType, opt => opt.MapFrom(src => src.TrackingType))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.UserMaintenanceTypes, opt => opt.Ignore());

            CreateMap<UserMaintenance, MaintenanceDTO>()
                .ForMember(dest => dest.MotorcycleId, opt => opt.MapFrom(src => src.MotorcycleId))
                .ForMember(dest => dest.KmInterval, opt => opt.MapFrom(src => src.KmInterval))
                .ForMember(dest => dest.TimeIntervalWeeks, opt => opt.MapFrom(src => src.TimeIntervalWeeks))
                .ForMember(dest => dest.TrackingType, opt => opt.MapFrom(src => src.TrackingType))
                .ForMember(dest => dest.IsSystem, opt => opt.Ignore());

            CreateMap<SaveMaintenanceDTO, UserMaintenance>()
                .ForMember(dest => dest.TrackingType, opt => opt.MapFrom(src => src.TrackingType))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.BaseType, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Motorcycle, opt => opt.Ignore())
                .ForMember(dest => dest.MaintenanceRecords, opt => opt.Ignore());

            CreateMap<MotorcycleMaintenanceRecord, MaintenanceRecordDTO>()
                .ForMember(dest => dest.MaintenanceName, opt => opt.MapFrom(src => src.UserMaintenance.Name));
        }
    }
}
