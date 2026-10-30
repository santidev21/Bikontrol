namespace Bikontrol.Application.Services
{
    /// <summary>Object key layout: one prefix per user, so a user's objects are easy to list and purge.</summary>
    public static class StorageKeys
    {
        public static string ForAttachment(Guid userId, Guid attachmentId, string contentType) =>
            $"attachments/{userId:N}/{attachmentId:N}.{ImageDataUrlValidator.ExtensionOf(contentType)}";
    }
}
