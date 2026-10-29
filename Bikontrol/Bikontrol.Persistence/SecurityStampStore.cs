using Bikontrol.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Bikontrol.Application.Options;
using Microsoft.Extensions.Options;

namespace Bikontrol.Persistence
{
    /// <summary>
    /// Looks the stamp up per request but caches it briefly, so a revoked token
    /// dies within <c>Jwt:SecurityStampCacheSeconds</c> (default 30) at the cost
    /// of one indexed read per user per window.
    /// </summary>
    public class SecurityStampStore : ISecurityStampStore
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl;

        public SecurityStampStore(AppDbContext context, IMemoryCache cache, IOptions<JwtOptions> options)
        {
            _context = context;
            _cache = cache;
            _ttl = TimeSpan.FromSeconds(options.Value.SecurityStampCacheSeconds);
        }

        public async Task<Guid?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var key = $"sstamp:{userId}";
            if (_ttl > TimeSpan.Zero && _cache.TryGetValue(key, out Guid? cached))
                return cached;

            var stamp = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => (Guid?)u.SecurityStamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (_ttl > TimeSpan.Zero)
                _cache.Set(key, stamp, _ttl);
            return stamp;
        }
    }
}
