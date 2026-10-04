using Bikontrol.Application.DTOs.Maintenance;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenancesController : ControllerBase
{
    private readonly IMaintenanceService _service;
    private readonly IMaintenanceAttachmentService _attachmentService;

    public MaintenancesController(IMaintenanceService service, IMaintenanceAttachmentService attachmentService)
    {
        _service = service;
        _attachmentService = attachmentService;
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

    [HttpGet("mine/motorcycle/{motorcycleId:guid}")]
    public async Task<IActionResult> GetMineByMotorcycle(Guid motorcycleId)
    {
        return Ok(await _service.GetUserMaintenanceByMotorcycleAsync(motorcycleId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _service.GetByIdAsync(id));
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
        var result = await _service.FollowDefaultAsync(request.MotorcycleId, request.DefaultId, request.KmInterval, request.TimeIntervalWeeks, request.TrackingType);
        return Ok(result);
    }

    [HttpPost("records")]
    public async Task<IActionResult> RegisterRecord([FromBody] CreateMaintenanceRecordRequest request)
    {
        var result = await _service.RegisterMaintenanceRecordAsync(request);
        return Ok(result);
    }

    [HttpGet("motorcycle/{motorcycleId:guid}/records")]
    public async Task<IActionResult> GetMotorcycleRecords(Guid motorcycleId)
    {
        var result = await _service.GetMaintenanceRecordsByMotorcycleAsync(motorcycleId);
        return Ok(result);
    }

    [HttpGet("motorcycle/{motorcycleId:guid}/upcoming")]
    public async Task<IActionResult> GetMotorcycleUpcoming(Guid motorcycleId)
    {
        var result = await _service.GetUpcomingByMotorcycleAsync(motorcycleId);
        return Ok(result);
    }

    [HttpGet("records/{recordId:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid recordId)
    {
        return Ok(await _attachmentService.GetByRecordAsync(recordId));
    }

    [HttpPost("records/{recordId:guid}/attachments")]
    public async Task<IActionResult> AddAttachment(Guid recordId, [FromBody] AddAttachmentRequest request)
    {
        return Ok(await _attachmentService.AddAsync(recordId, request));
    }

    [HttpDelete("records/{recordId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid recordId, Guid attachmentId)
    {
        await _attachmentService.DeleteAsync(recordId, attachmentId);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveMaintenanceDTO dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }
}
