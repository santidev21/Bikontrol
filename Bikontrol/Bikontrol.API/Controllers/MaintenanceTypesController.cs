using Bikontrol.Application.DTOs.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceTypesController : ControllerBase
{
    private readonly IMaintenanceTypeService _service;

    public MaintenanceTypesController(IMaintenanceTypeService service)
    {
        _service = service;
    }

    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaults()
    {
        return Ok(await _service.GetDefaultsAsync());
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyTypes()
    {
        return Ok(await _service.GetUserTypesAsync());
    }

    [HttpPost("mine")]
    public async Task<IActionResult> CreateUserType([FromBody] CreateMaintenanceTypeDTO dto)
    {
        return Ok(await _service.CreateUserTypeAsync(dto));
    }

    [HttpDelete("mine/{id}")]
    public async Task<IActionResult> DeleteUserType(Guid id)
    {
        await _service.DeleteUserTypeAsync(id);
        return NoContent();
    }

    [HttpPatch("mine/{id}/disable")]
    public async Task<IActionResult> DisableUserType(Guid id)
    {
        await _service.DisableUserTypeAsync(id);
        return NoContent();
    }
}
