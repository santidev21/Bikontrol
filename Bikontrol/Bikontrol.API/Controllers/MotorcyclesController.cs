using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bikontrol.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MotorcyclesController : ControllerBase
    {
        private readonly IMotorcycleService _motorcycleService;

        public MotorcyclesController(IMotorcycleService motorcycleService)
        {
            _motorcycleService = motorcycleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveMotorcycleDTO dto)
        {
            var result = await _motorcycleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _motorcycleService.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyMotorcycles()
        {
            var result = await _motorcycleService.GetByCurrentUserAsync();
            return Ok(result);
        }

        [HttpGet("{id}/km/current")]
        public async Task<IActionResult> GetCurrentKm(Guid id)
        {
            var result = await _motorcycleService.GetCurrentKmAsync(id);
            return Ok(new { km = result });
        }

        [HttpPost("{id}/km-history")]
        public async Task<IActionResult> AddKmHistory(Guid id, [FromBody] AddKmHistoryRequest request)
        {
            await _motorcycleService.AddKmHistoryAsync(id, request.Km);
            return NoContent();
        }

        [HttpDelete("{id}/km-history/last")]
        public async Task<IActionResult> RollbackLastKm(Guid id, [FromBody] RollbackKmHistoryRequest request)
        {
            await _motorcycleService.RollbackLastKmAsync(id, request.NewKm);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveMotorcycleDTO dto)
        {
            await _motorcycleService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _motorcycleService.SoftDeleteAsync(id);
            return NoContent();
        }
    }
}
