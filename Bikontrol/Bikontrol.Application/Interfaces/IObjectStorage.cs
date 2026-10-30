namespace Bikontrol.Application.Interfaces
{
    /// <summary>A stored object opened for reading; disposing it closes <see cref="Content"/>.</summary>
    public sealed record StoredObject(Stream Content, string ContentType, long Length) : IDisposable
    {
        public void Dispose() => Content.Dispose();
    }

    /// <summary>
    /// Port for the private bucket that holds user images. Keys are opaque to the
    /// adapter; the application builds them (<c>attachments/{userId}/{id}.{ext}</c>).
    /// </summary>
    public interface IObjectStorage
    {
        /// <summary>False when no bucket is configured: images then stay in the database.</summary>
        bool IsEnabled { get; }

        Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default);

        /// <summary>Opens an object, or returns null when the key does not exist.</summary>
        Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Deletes objects; missing keys are ignored.</summary>
        Task DeleteAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    }
}
