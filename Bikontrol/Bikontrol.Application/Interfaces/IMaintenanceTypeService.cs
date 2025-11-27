using Bikontrol.Application.DTOs.Maintenance;

public interface IMaintenanceTypeService
{
    Task<IEnumerable<MaintenanceTypeDTO>> GetDefaultsAsync();
    Task<IEnumerable<MaintenanceTypeDTO>> GetUserTypesAsync();

    Task<MaintenanceTypeDTO> CreateUserTypeAsync(CreateMaintenanceTypeDTO dto);

    Task DeleteUserTypeAsync(Guid id);
    Task DisableUserTypeAsync(Guid id);
}
