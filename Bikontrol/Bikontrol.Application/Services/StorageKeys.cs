namespace Bikontrol.Application.Services
{
    /// <summary>Object key layout: one prefix per user, so a user's objects are easy to list and purge.</summary>
    public static class StorageKeys
    {
        public static string ForAttachment(Guid userId, Guid attachmentId, string contentType) =>
            $"attachments/{userId:N}/{attachmentId:N}.{ImageDataUrlValidator.ExtensionOf(contentType)}";

        /// <summary>One object per photo version, so replacing a photo never overwrites a cached one.</summary>
        public static string ForMotorcycleImage(Guid userId, Guid motorcycleId, string version, string contentType) =>
            $"motorcycles/{userId:N}/{motorcycleId:N}/{version}.{ImageDataUrlValidator.ExtensionOf(contentType)}";

        /// <summary>MIME type of a stored photo, from the extension its key was created with.</summary>
        public static string ContentTypeOfKey(string key) => key.EndsWith(".png", StringComparison.Ordinal) ? "image/png"
            : key.EndsWith(".webp", StringComparison.Ordinal) ? "image/webp"
            : "image/jpeg";
    }
}
