using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Domain.Entities;

public interface IMaintenanceService
{
    Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync();
    Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync();
    Task<MaintenanceDTO?> GetByIdAsync(Guid id);
    Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto);
    Task DeleteUserMaintenanceAsync(Guid id);
    Task<MaintenanceDTO> FollowDefaultAsync(Guid defaultId, int? KmInterval, int? TimeIntervalWeeks, string TrackingType);
    Task UpdateAsync(Guid id, SaveMaintenanceDTO dto);
}
