using System.ComponentModel.DataAnnotations;

namespace Bikontrol.Application.Options
{
    /// <summary>
    /// Object storage for user images (<c>Storage</c> section). Any S3-compatible
    /// service works (Cloudflare R2, Backblaze B2, MinIO, AWS S3). Disabled by
    /// default: images then stay in the database, as before.
    /// </summary>
    public class StorageOptions : IValidatableObject
    {
        public const string Section = "Storage";

        public bool Enabled { get; set; }

        /// <summary>S3 endpoint, e.g. <c>https://&lt;account&gt;.r2.cloudflarestorage.com</c>.</summary>
        public string? ServiceUrl { get; set; }

        public string? Bucket { get; set; }

        public string? AccessKey { get; set; }

        public string? SecretKey { get; set; }

        /// <summary>Signing region; R2 uses <c>auto</c>.</summary>
        public string Region { get; set; } = "auto";

        /// <summary>Path-style addressing (<c>host/bucket/key</c>), required by MinIO.</summary>
        public bool ForcePathStyle { get; set; } = true;

        /// <summary>
        /// Per-user cap on stored attachment bytes. 0 disables the quota. Applies to
        /// images in the database and in the bucket alike.
        /// </summary>
        [Range(0, long.MaxValue)]
        public long MaxBytesPerUser { get; set; } = 50L * 1024 * 1024;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enabled)
                yield break;

            if (!Uri.TryCreate(ServiceUrl, UriKind.Absolute, out _))
                yield return new ValidationResult("Storage:ServiceUrl must be an absolute URL when storage is enabled.", new[] { nameof(ServiceUrl) });
            if (string.IsNullOrWhiteSpace(Bucket))
                yield return new ValidationResult("Storage:Bucket is required when storage is enabled.", new[] { nameof(Bucket) });
            if (string.IsNullOrWhiteSpace(AccessKey) || string.IsNullOrWhiteSpace(SecretKey))
                yield return new ValidationResult("Storage:AccessKey and Storage:SecretKey are required when storage is enabled.", new[] { nameof(AccessKey) });
        }
    }
}
