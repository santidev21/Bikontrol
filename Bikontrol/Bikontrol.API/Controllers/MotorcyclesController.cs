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
        public async Task<IActionResult> Create([FromBody] CreateMotorcycleDTO dto)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMotorcycleDTO dto)
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
