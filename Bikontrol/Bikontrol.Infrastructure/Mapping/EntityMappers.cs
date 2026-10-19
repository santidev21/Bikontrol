using Bikontrol.Application.DTOs.Auth;
using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.DTOs.Users;
using Bikontrol.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Bikontrol.Infrastructure.Mapping
{
    /// <summary>
    /// Source-generated entity to DTO mappings (Riok.Mapperly). Compile-time
    /// checked: a new DTO property without a mapping rule surfaces an
    /// unmapped-member diagnostic here, instead of silently producing default
    /// data at runtime. Service-minted members (tokens, expiry, km) are
    /// ignored on purpose; the services fill them after mapping.
    /// </summary>
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public static partial class EntityMappers
    {
        // AuthService mints tokens and the confirmation flag after mapping.
        [MapperIgnoreTarget(nameof(RegisterResponse.Token))]
        [MapperIgnoreTarget(nameof(RegisterResponse.RefreshToken))]
        [MapperIgnoreTarget(nameof(RegisterResponse.ExpiresIn))]
        [MapperIgnoreTarget(nameof(RegisterResponse.EmailConfirmationRequired))]
        public static partial RegisterResponse ToRegisterResponse(this User user);

        [MapperIgnoreTarget(nameof(LoginResponse.Token))]
        [MapperIgnoreTarget(nameof(LoginResponse.RefreshToken))]
        [MapperIgnoreTarget(nameof(LoginResponse.ExpiresIn))]
        public static partial LoginResponse ToLoginResponse(this User user);

        public static partial ProfileDTO ToProfileDTO(this User user);

        // Km is resolved from the km history, not from the entity; the service
        // fills it in.
        [MapperIgnoreTarget(nameof(MotorcycleDTO.Km))]
        public static partial MotorcycleDTO ToMotorcycleDTO(this Motorcycle motorcycle);

        public static partial List<MotorcycleDTO> ToMotorcycleDTOs(this IEnumerable<Motorcycle> motorcycles);

        [MapperIgnoreTarget(nameof(MaintenanceDTO.MotorcycleId))]
        [MapperIgnoreTarget(nameof(MaintenanceDTO.BaseTypeId))]
        [MapperIgnoreTarget(nameof(MaintenanceDTO.IsSystem))]
        [MapProperty(nameof(Maintenance.DefaultKmInterval), nameof(MaintenanceDTO.KmInterval))]
        [MapProperty(nameof(Maintenance.DefaultTimeIntervalWeeks), nameof(MaintenanceDTO.TimeIntervalWeeks))]
        public static partial MaintenanceDTO ToMaintenanceDTO(this Maintenance defaultType);

        public static partial List<MaintenanceDTO> ToMaintenanceDTOs(this IEnumerable<Maintenance> defaultTypes);

        [MapperIgnoreTarget(nameof(MaintenanceDTO.IsSystem))]
        public static partial MaintenanceDTO ToMaintenanceDTO(this UserMaintenance maintenance);

        public static partial List<MaintenanceDTO> ToMaintenanceDTOs(this IEnumerable<UserMaintenance> userMaintenances);

        // Hand-written instead of generated: the navigation may be unloaded (a
        // freshly created record, or unmapped test data) and the generated
        // source path would throw a NullReferenceException, while the previous
        // AutoMapper behaviour mapped a null nav to the property default.
        public static MaintenanceRecordDTO ToMaintenanceRecordDTO(this MotorcycleMaintenanceRecord record)
        {
            return new MaintenanceRecordDTO
            {
                Id = record.Id,
                MotorcycleId = record.MotorcycleId,
                UserMaintenanceId = record.UserMaintenanceId,
                PerformedAt = record.PerformedAt,
                PerformedKm = record.PerformedKm,
                Cost = record.Cost,
                CreatedAt = record.CreatedAt,
                MaintenanceName = record.UserMaintenance?.Name ?? string.Empty
            };
        }

        public static partial List<MaintenanceRecordDTO> ToMaintenanceRecordDTOs(this IEnumerable<MotorcycleMaintenanceRecord> records);

        [MapProperty(nameof(MaintenanceRecordAttachment.MotorcycleMaintenanceRecordId), nameof(MaintenanceAttachmentDTO.RecordId))]
        public static partial MaintenanceAttachmentDTO ToAttachmentDTO(this MaintenanceRecordAttachment attachment);

        public static partial List<MaintenanceAttachmentDTO> ToAttachmentDTOs(this IEnumerable<MaintenanceRecordAttachment> attachments);

        public static partial FuelUpDTO ToFuelUpDTO(this FuelUp fuelUp);

        public static partial IEnumerable<FuelUpDTO> ToFuelUpDTOs(this IEnumerable<FuelUp> fuelUps);
    }
}
