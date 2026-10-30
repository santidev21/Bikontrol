using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Bikontrol.API.Controllers
{
    /// <summary>
    /// Streams motorcycle photos behind signed, expiring URLs. Anonymous for the same
    /// reason as <see cref="AttachmentMediaController"/>: the signature is the credential.
    /// </summary>
    [ApiController]
    [Route("api/media/motorcycles")]
    [AllowAnonymous]
    public class MotorcycleMediaController : ControllerBase
    {
        private readonly IMotorcycleService _motorcycleService;

        public MotorcycleMediaController(IMotorcycleService motorcycleService)
        {
            _motorcycleService = motorcycleService;
        }

        /// <param name="v">Photo version: only busts the browser cache, it is not signed.</param>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, [FromQuery] long exp, [FromQuery] string? sig, [FromQuery] string? v, CancellationToken cancellationToken)
        {
            var media = await _motorcycleService.OpenImageAsync(id, exp, sig, cancellationToken);

            // A new photo gets a new version, hence a new URL and ETag; until then the bytes are immutable.
            Response.Headers.CacheControl = "private, max-age=21600";
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(media.Content, media.ContentType, lastModified: null, entityTag: new EntityTagHeaderValue($"\"{id:N}-{v}\""));
        }
    }
}
