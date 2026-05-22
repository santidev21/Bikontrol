using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Domain.Entities;

public interface IMaintenanceService
{
    Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync();
    Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync();
    Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceByMotorcycleAsync(Guid motorcycleId);
    Task<MaintenanceDTO?> GetByIdAsync(Guid id);
    Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto);
    Task DeleteUserMaintenanceAsync(Guid id);
    Task<MaintenanceDTO> FollowDefaultAsync(Guid motorcycleId, Guid defaultId, int? KmInterval, int? TimeIntervalWeeks, string TrackingType);
    Task UpdateAsync(Guid id, SaveMaintenanceDTO dto);
    Task<MaintenanceRecordDTO> RegisterMaintenanceRecordAsync(CreateMaintenanceRecordRequest request);
    Task<IEnumerable<MaintenanceRecordDTO>> GetMaintenanceRecordsByMotorcycleAsync(Guid motorcycleId);
    Task<IEnumerable<UpcomingMaintenanceDTO>> GetUpcomingByMotorcycleAsync(Guid motorcycleId);
}
