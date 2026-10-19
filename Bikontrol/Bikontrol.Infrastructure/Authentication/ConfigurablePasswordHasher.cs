using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;

namespace Bikontrol.Infrastructure.Authentication
{
    /// <summary>
    /// PasswordHasher with a configurable PBKDF2 work factor. OWASP currently
    /// recommends 600k iterations for PBKDF2-HMAC-SHA256 (Identity's v3 stock
    /// default is 100k); the exact value stays a CPU/latency trade-off, so it
    /// is injected rather than hardcoded.
    /// The work factor is passed through the official PasswordHasherOptions
    /// knob: Identity's v3 format embeds the iteration count in every hash, so
    /// the inherited verification keeps accepting hashes minted with older
    /// factors (and legacy stock-hasher hashes) without any migration.
    /// </summary>
    public sealed class ConfigurablePasswordHasher<TUser> : PasswordHasher<TUser> where TUser : class
    {
        public const int MinimumIterations = 100_000;

        public ConfigurablePasswordHasher(int iterations)
            : base(Options.Create(CreateOptions(iterations)))
        {
        }

        private static PasswordHasherOptions CreateOptions(int iterations)
        {
            if (iterations < MinimumIterations)
                throw new ArgumentOutOfRangeException(
                    nameof(iterations), $"Iterations must be at least {MinimumIterations}.");

            return new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = iterations
            };
        }
    }
}
