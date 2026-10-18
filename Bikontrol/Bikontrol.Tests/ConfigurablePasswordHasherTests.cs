using Bikontrol.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Buffers.Binary;

namespace Bikontrol.Tests;

public class ConfigurablePasswordHasherTests
{
    private const int FastIterations = 100_001;

    [Fact]
    public void HashPassword_ShouldRoundTripWithTheInheritedVerifier()
    {
        var hasher = new ConfigurablePasswordHasher<object>(FastIterations);
        var hash = hasher.HashPassword(null!, "correct horse battery staple");

        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(null!, hash, "correct horse battery staple"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(null!, hash, "wrong password"));
    }

    [Fact]
    public void HashPassword_ShouldEmbedTheConfiguredIterationCount()
    {
        var hasher = new ConfigurablePasswordHasher<object>(FastIterations);
        var hash = hasher.HashPassword(null!, "secret");

        // Identity v3 layout: marker(1) prf(4) iterations(4) saltLen(4) then
        // salt and subkey — there is no subkey-length field in the header.
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(0x01, bytes[0]);
        // The embedded PRF constant for HMAC-SHA256 in Identity's v3 header is 2
        // (Identity's internal enum ordering, not the public KeyDerivationPrf).
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4)));
        Assert.Equal(FastIterations, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(5, 4)));
        Assert.Equal(16, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(9, 4))); // salt
        // The subkey is the remaining tail: 13 header + 16 salt + 32 subkey.
        Assert.Equal(61, bytes.Length);
    }

    [Fact]
    public void VerifyHashedPassword_ShouldAcceptHashesMintedByTheStockHasher()
    {
        // The iteration count is embedded per hash, so legacy 100k hashes keep
        // authenticating after the work factor is raised — Identity answers
        // SuccessRehashNeeded (verify succeeded; rehash on next login).
        var legacyHasher = new PasswordHasher<object>();
        var legacyHash = legacyHasher.HashPassword(null!, "legacy password");

        var hasher = new ConfigurablePasswordHasher<object>(600_000);

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded,
            hasher.VerifyHashedPassword(null!, legacyHash, "legacy password"));
    }

    [Fact]
    public void HashPassword_ShouldSaltEveryHash()
    {
        var hasher = new ConfigurablePasswordHasher<object>(FastIterations);
        var first = hasher.HashPassword(null!, "same password");
        var second = hasher.HashPassword(null!, "same password");

        // Unique salt per hash, and the password never appears in the output.
        Assert.NotEqual(first, second);
        Assert.DoesNotContain("same password", first);
        Assert.DoesNotContain("same password", second);
    }

    [Fact]
    public void Constructor_WhenIterationsBelowTheFloor_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConfigurablePasswordHasher<object>(99_999));
    }

    [Fact]
    public void HashPassword_ShouldNotDependOnTheUserInstance()
    {
        var hasher = new ConfigurablePasswordHasher<object>(FastIterations);

        var hash = hasher.HashPassword(null!, "password");

        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(null!, hash, "password"));
    }
}
