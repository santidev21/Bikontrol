using Bikontrol.Application.DTOs.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenancesController : ControllerBase
{
    private readonly IMaintenanceService _service;

    public MaintenancesController(IMaintenanceService service)
    {
        _service = service;
    }

    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaults()
    {
        return Ok(await _service.GetDefaultsAsync());
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMys()
    {
        return Ok(await _service.GetUserMaintenanceAsync());
    }

    [HttpPost("mine")]
    public async Task<IActionResult> CreateUser([FromBody] SaveMaintenanceDTO dto)
    {
        return Ok(await _service.CreateUserMaintenanceAsync(dto));
    }

    [HttpDelete("mine/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _service.DeleteUserMaintenanceAsync(id);
        return NoContent();
    }

    [HttpPost("follow")]
    public async Task<IActionResult> FollowDefault([FromBody] FollowDefaultRequest request)
    {
        var result = await _service.FollowDefaultAsync(request.DefaultId);
        return Ok(result);
    }
}
