using Bikontrol.Application.DTOs.Maintenance;

public interface IMaintenanceService
{
    Task<IEnumerable<MaintenanceDTO>> GetDefaultsAsync();
    Task<IEnumerable<MaintenanceDTO>> GetUserMaintenanceAsync();

    Task<MaintenanceDTO> CreateUserMaintenanceAsync(SaveMaintenanceDTO dto);

    Task DeleteUserMaintenanceAsync(Guid id);
}
