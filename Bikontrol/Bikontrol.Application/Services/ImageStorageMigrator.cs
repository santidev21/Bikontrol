using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Bikontrol.Application.Services
{
    /// <summary>Outcome of one migration batch.</summary>
    public sealed record StorageMigrationBatch(int Migrated, int Failed);

    /// <summary>
    /// Moves attachment images and motorcycle photos that still live as data URLs
    /// in the database into object storage. Idempotent and resumable: a row is only
    /// updated after its upload succeeded, and re-uploading the same key after a
    /// crash is harmless. Run it from a fresh DI scope per batch so the change
    /// tracker never accumulates megabytes of base64.
    /// </summary>
    public sealed class ImageStorageMigrator
    {
        private readonly IMaintenanceRecordAttachmentRepository _attachments;
        private readonly IMotorcycleRepository _motorcycles;
        private readonly IObjectStorage _storage;
        private readonly ILogger<ImageStorageMigrator> _logger;

        public ImageStorageMigrator(
            IMaintenanceRecordAttachmentRepository attachments,
            IMotorcycleRepository motorcycles,
            IObjectStorage storage,
            ILogger<ImageStorageMigrator> logger)
        {
            _attachments = attachments;
            _motorcycles = motorcycles;
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
                        LogNoOwner(_logger, attachment.Id, null);
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
                    LogMigrationFailed(_logger, attachment.Id, ex);
                    failed++;
                }
            }

            var motorcycles = await _motorcycles.GetPendingImageMigrationAsync(batchSize);
            foreach (var motorcycle in motorcycles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    ImageDataUrlValidator.Validate(motorcycle.Image, required: true);
                    var bytes = ImageDataUrlValidator.Decode(motorcycle.Image);
                    var contentType = ImageDataUrlValidator.ContentTypeOf(motorcycle.Image);
                    var version = Guid.NewGuid().ToString("N")[..12];
                    var key = StorageKeys.ForMotorcycleImage(motorcycle.UserId, motorcycle.Id, version, contentType);

                    await _storage.PutAsync(key, bytes, contentType, cancellationToken);

                    motorcycle.ImageKey = key;
                    motorcycle.ImageVersion = version;
                    motorcycle.ImageSizeBytes = bytes.Length;
                    motorcycle.Image = "default.png";
                    await _motorcycles.SaveChangesAsync();
                    migrated++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogMotorcycleMigrationFailed(_logger, motorcycle.Id, ex);
                    failed++;
                }
            }

            return new StorageMigrationBatch(migrated, failed);
        }

        private static readonly Action<ILogger, Guid, Exception?> LogNoOwner = LoggerMessage.Define<Guid>(
            LogLevel.Warning, new EventId(1, nameof(LogNoOwner)), "Attachment {AttachmentId} has no owner; skipped.");

        private static readonly Action<ILogger, Guid, Exception?> LogMigrationFailed = LoggerMessage.Define<Guid>(
            LogLevel.Warning, new EventId(2, nameof(LogMigrationFailed)), "Could not migrate attachment {AttachmentId}; it stays in the database.");

        private static readonly Action<ILogger, Guid, Exception?> LogMotorcycleMigrationFailed = LoggerMessage.Define<Guid>(
            LogLevel.Warning, new EventId(3, nameof(LogMotorcycleMigrationFailed)), "Could not migrate the photo of motorcycle {MotorcycleId}; it stays in the database.");
    }
}
