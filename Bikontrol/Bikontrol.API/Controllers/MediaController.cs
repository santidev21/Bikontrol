using Bikontrol.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Bikontrol.API.Controllers
{
    /// <summary>
    /// Streams stored images behind signed, expiring URLs (see
    /// <see cref="IMediaUrlSigner"/>). Anonymous because an <c>&lt;img&gt;</c> tag
    /// cannot send a bearer token; the HMAC signature is the credential and the
    /// bucket itself stays private.
    /// </summary>
    [ApiController]
    [Route("api/media")]
    [AllowAnonymous]
    public class MediaController : ControllerBase
    {
        private readonly IMaintenanceAttachmentService _attachmentService;

        public MediaController(IMaintenanceAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpGet("attachments/{id:guid}")]
        public async Task<IActionResult> GetAttachment(Guid id, [FromQuery] long exp, [FromQuery] string? sig, CancellationToken cancellationToken)
        {
            var media = await _attachmentService.OpenMediaAsync(id, exp, sig, cancellationToken);

            // The bytes behind an id never change, so the id is a perfect ETag; the
            // signed URL is stable for a 6-hour window, which lets the browser cache.
            Response.Headers.CacheControl = "private, max-age=21600";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(media.Content, media.ContentType, lastModified: null, entityTag: new EntityTagHeaderValue($"\"{id:N}\""));
        }
    }
}
