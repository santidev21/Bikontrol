using Bikontrol.Application.Interfaces;

namespace Bikontrol.Infrastructure.Storage
{
    /// <summary>Used when <c>Storage:Enabled</c> is false: images stay in the database.</summary>
    public sealed class NoopObjectStorage : IObjectStorage
    {
        public bool IsEnabled => false;

        public Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Object storage is not enabled (Storage:Enabled=false).");

        public Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredObject?>(null);

        public Task DeleteAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
