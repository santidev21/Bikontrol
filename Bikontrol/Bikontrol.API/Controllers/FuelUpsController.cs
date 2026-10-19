using Bikontrol.Application.DTOs.Fuel;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.API.Controllers
{
    /// <summary>
    /// Fuel-ups (refuelling log) and the consumption / cost metrics derived from
    /// them. The log lives under a motorcycle, like maintenance records.
    /// </summary>
    [ApiController]
    [Route("api/fuel-ups")]
    [Authorize]
    public class FuelUpsController : ControllerBase
    {
        private readonly IFuelUpService _service;

        public FuelUpsController(IFuelUpService service)
        {
            _service = service;
        }

        [HttpGet("motorcycle/{motorcycleId:guid}")]
        public async Task<IActionResult> GetByMotorcycle(Guid motorcycleId, [FromQuery] int? limit)
        {
            return Ok(await _service.GetByMotorcycleAsync(motorcycleId, ListLimits.Clamp(limit)));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _service.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveFuelUpRequest request)
        {
            return Ok(await _service.CreateAsync(request));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveFuelUpRequest request)
        {
            return Ok(await _service.UpdateAsync(id, request));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("motorcycle/{motorcycleId:guid}/consumption")]
        public async Task<IActionResult> GetConsumption(Guid motorcycleId)
        {
            return Ok(await _service.GetConsumptionAsync(motorcycleId));
        }
    }
}
