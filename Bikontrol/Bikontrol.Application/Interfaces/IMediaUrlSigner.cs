namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Signed, expiring same-origin URLs for stored media. The bucket stays
    /// private: the API streams the bytes after checking the signature, so no
    /// bucket CORS or CSP change is needed and <c>&lt;img src&gt;</c> works
    /// without an Authorization header.
    /// </summary>
    public interface IMediaUrlSigner
    {
        /// <summary>
        /// A path such as <c>/api/media/attachments/{id}?exp=…&amp;sig=…</c>. The
        /// expiry is rounded to a time window, so the URL is identical for the
        /// whole window and the browser can cache the image.
        /// </summary>
        string CreateAttachmentUrl(Guid attachmentId);

        bool IsValidAttachmentUrl(Guid attachmentId, long expiresUnix, string? signature);

        /// <summary>
        /// <c>/api/media/motorcycles/{id}?v=…&amp;exp=…&amp;sig=…</c>. <paramref name="version"/>
        /// changes with every new photo and is part of the URL only to bust the
        /// browser cache; it is not signed because it grants nothing.
        /// </summary>
        string CreateMotorcycleUrl(Guid motorcycleId, string? version);

        bool IsValidMotorcycleUrl(Guid motorcycleId, long expiresUnix, string? signature);
    }
}
