using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikontrol.Application.Services
{
    /// <summary>Outcome of one migration batch.</summary>
    public sealed record StorageMigrationBatch(int Migrated, int Failed);

    /// <summary>
    /// Moves attachments that still hold their image as a data URL in the
    /// database into object storage. Idempotent and resumable: a row is only
    /// updated after its upload succeeded, and re-uploading the same key after a
    /// crash is harmless. Run it from a fresh DI scope per batch so the change
    /// tracker never accumulates megabytes of base64.
    /// </summary>
    public sealed partial class AttachmentStorageMigrator
    {
        private readonly IMaintenanceRecordAttachmentRepository _attachments;
        private readonly IObjectStorage _storage;
        private readonly ILogger<AttachmentStorageMigrator> _logger;

        public AttachmentStorageMigrator(
            IMaintenanceRecordAttachmentRepository attachments,
            IObjectStorage storage,
            ILogger<AttachmentStorageMigrator> logger)
        {
            _attachments = attachments;
            _storage = storage;
            _logger = logger;
        }

        public async Task<StorageMigrationBatch> MigrateBatchAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            if (!_storage.IsEnabled)
                throw new InvalidOperationException("Object storage is not enabled (set Storage:Enabled=true and its credentials).");

            var pending = await _attachments.GetPendingStorageMigrationAsync(batchSize);
            int migrated = 0, failed = 0;

            foreach (var attachment in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var owner = await _attachments.GetOwnerIdAsync(attachment.Id);
                    if (owner is null)
                    {
                        LogNoOwner(_logger, attachment.Id);
                        failed++;
                        continue;
                    }

                    ImageDataUrlValidator.Validate(attachment.DataUrl, required: true);
                    var bytes = ImageDataUrlValidator.Decode(attachment.DataUrl);
                    var contentType = ImageDataUrlValidator.ContentTypeOf(attachment.DataUrl);
                    var key = StorageKeys.ForAttachment(owner.Value, attachment.Id, contentType);

                    await _storage.PutAsync(key, bytes, contentType, cancellationToken);

                    attachment.StorageKey = key;
                    attachment.ContentType = contentType;
                    attachment.SizeBytes = bytes.Length;
                    attachment.DataUrl = string.Empty;
                    await _attachments.SaveChangesAsync();
                    migrated++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogMigrationFailed(_logger, ex, attachment.Id);
                    failed++;
                }
            }

            return new StorageMigrationBatch(migrated, failed);
        }

        [LoggerMessage(Level = LogLevel.Warning, Message = "Attachment {AttachmentId} has no owner; skipped.")]
        private static partial void LogNoOwner(ILogger logger, Guid attachmentId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not migrate attachment {AttachmentId}; it stays in the database.")]
        private static partial void LogMigrationFailed(ILogger logger, Exception exception, Guid attachmentId);
    }
}
