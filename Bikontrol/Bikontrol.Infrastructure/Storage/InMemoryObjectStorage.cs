using System.Collections.Concurrent;
using Bikontrol.Application.Interfaces;

namespace Bikontrol.Infrastructure.Storage
{
    /// <summary>
    /// Process-local storage for tests and throwaway local runs. Nothing survives
    /// a restart, so it must never back a real deployment.
    /// </summary>
    public sealed class InMemoryObjectStorage : IObjectStorage
    {
        private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _objects = new();

        public bool IsEnabled => true;

        public IReadOnlyCollection<string> GetKeys() => _objects.Keys.ToList();

        public Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
        {
            _objects[key] = (content.ToArray(), contentType);
            return Task.CompletedTask;
        }

        public Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            StoredObject? result = _objects.TryGetValue(key, out var found)
                ? new StoredObject(new MemoryStream(found.Content, writable: false), found.ContentType, found.Content.Length)
                : null;
            return Task.FromResult(result);
        }

        public Task DeleteAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _objects.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }
    }
}
