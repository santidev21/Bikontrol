using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Application.Options;
using Bikontrol.Application.Services;
using Bikontrol.Domain.Entities;
using Bikontrol.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bikontrol.Tests;

public class ObjectStorageTests
{
    private const string PngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private static MediaUrlSigner SignerAt(DateTimeOffset now) =>
        new(Options.Create(new JwtOptions { Key = new string('k', 40) }), () => now);

    private static (long Exp, string Sig) Parse(string url)
    {
        var query = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + url).Query);
        return (long.Parse(query["exp"]!), query["sig"]!);
    }

    [Fact]
    public void SignedUrl_IsStableWithinAWindowSoTheBrowserCanCacheIt()
    {
        var id = Guid.NewGuid();
        var windowStart = DateTimeOffset.FromUnixTimeSeconds(MediaUrlSigner.WindowSeconds * 1000L);

        var first = SignerAt(windowStart.AddMinutes(1)).CreateAttachmentUrl(id);
        var later = SignerAt(windowStart.AddHours(5)).CreateAttachmentUrl(id);
        var nextWindow = SignerAt(windowStart.AddHours(7)).CreateAttachmentUrl(id);

        Assert.Equal(first, later);
        Assert.NotEqual(first, nextWindow);
    }

    [Fact]
    public void SignedUrl_LivesBetweenOneAndTwoWindowsAndThenExpires()
    {
        var id = Guid.NewGuid();
        var created = DateTimeOffset.FromUnixTimeSeconds(MediaUrlSigner.WindowSeconds * 1000L + 60);
        var (exp, sig) = Parse(SignerAt(created).CreateAttachmentUrl(id));

        Assert.True(SignerAt(created.AddHours(5)).IsValidAttachmentUrl(id, exp, sig));
        Assert.True(SignerAt(created.AddHours(11)).IsValidAttachmentUrl(id, exp, sig));
        Assert.False(SignerAt(created.AddHours(13)).IsValidAttachmentUrl(id, exp, sig));
    }

    [Fact]
    public void SignedUrl_IsBoundToItsIdAndKey()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var (exp, sig) = Parse(SignerAt(now).CreateAttachmentUrl(id));
        var otherKey = new MediaUrlSigner(Options.Create(new JwtOptions { Key = new string('z', 40) }), () => now);

        Assert.False(SignerAt(now).IsValidAttachmentUrl(Guid.NewGuid(), exp, sig));
        Assert.False(otherKey.IsValidAttachmentUrl(id, exp, sig));
        Assert.False(SignerAt(now).IsValidAttachmentUrl(id, exp, string.Empty));
    }

    [Fact]
    public async Task InMemoryStorage_RoundTripsAndIgnoresMissingKeys()
    {
        var storage = new InMemoryObjectStorage();
        await storage.PutAsync("k", new byte[] { 9 }, "image/png");

        using (var found = await storage.GetAsync("k"))
        {
            Assert.NotNull(found);
            Assert.Equal("image/png", found!.ContentType);
            Assert.Equal(1, found.Length);
        }

        await storage.DeleteAsync(new[] { "k", "never-existed" });
        Assert.Null(await storage.GetAsync("k"));
    }

    [Fact]
    public async Task NoopStorage_IsDisabledAndRefusesWrites()
    {
        var storage = new NoopObjectStorage();

        Assert.False(storage.IsEnabled);
        Assert.Null(await storage.GetAsync("k"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.PutAsync("k", new byte[1], "image/png"));
    }

    [Fact]
    public void Options_WhenEnabledRequireTheBucketSettings()
    {
        var disabled = Validate(new StorageOptions());
        var incomplete = Validate(new StorageOptions { Enabled = true });
        var complete = Validate(new StorageOptions
        {
            Enabled = true,
            ServiceUrl = "https://acc.r2.cloudflarestorage.com",
            Bucket = "bikontrol",
            AccessKey = "a",
            SecretKey = "b"
        });

        Assert.Empty(disabled);
        Assert.Equal(3, incomplete.Count);
        Assert.Empty(complete);
    }

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> Validate(StorageOptions options) =>
        options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();

    [Fact]
    public async Task Migrator_MovesLegacyImagesToStorageAndIsIdempotent()
    {
        var owner = Guid.NewGuid();
        var legacy = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), DataUrl = PngDataUrl, ContentType = "image/png" };
        var repo = new FakeRepo(owner, legacy);
        var storage = new InMemoryObjectStorage();
        var migrator = new AttachmentStorageMigrator(repo, storage, NullLogger<AttachmentStorageMigrator>.Instance);

        var first = await migrator.MigrateBatchAsync(10);
        var second = await migrator.MigrateBatchAsync(10);

        Assert.Equal(new StorageMigrationBatch(1, 0), first);
        Assert.Equal(new StorageMigrationBatch(0, 0), second);
        Assert.Equal($"attachments/{owner:N}/{legacy.Id:N}.png", legacy.StorageKey);
        Assert.Equal(string.Empty, legacy.DataUrl);
        Assert.Equal(ImageDataUrlValidator.Decode(PngDataUrl).Length, legacy.SizeBytes);
        Assert.Equal(new[] { legacy.StorageKey }, storage.Keys);
    }

    [Fact]
    public async Task Migrator_KeepsRowsThatCannotBeMigratedInTheDatabase()
    {
        var broken = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), DataUrl = "data:text/plain;base64,aGk=" };
        var orphan = new MaintenanceRecordAttachment { Id = Guid.NewGuid(), DataUrl = PngDataUrl };
        var repo = new FakeRepo(Guid.NewGuid(), broken, orphan) { NoOwnerFor = orphan.Id };
        var storage = new InMemoryObjectStorage();
        var migrator = new AttachmentStorageMigrator(repo, storage, NullLogger<AttachmentStorageMigrator>.Instance);

        var result = await migrator.MigrateBatchAsync(10);

        Assert.Equal(new StorageMigrationBatch(0, 2), result);
        Assert.Null(broken.StorageKey);
        Assert.Null(orphan.StorageKey);
        Assert.Empty(storage.Keys);
    }

    [Fact]
    public async Task Migrator_RefusesToRunWithoutStorage()
    {
        var migrator = new AttachmentStorageMigrator(new FakeRepo(Guid.NewGuid()), new NoopObjectStorage(), NullLogger<AttachmentStorageMigrator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.MigrateBatchAsync(10));
    }

    private sealed class FakeRepo : IMaintenanceRecordAttachmentRepository
    {
        private readonly Guid _owner;
        private readonly List<MaintenanceRecordAttachment> _items;

        public FakeRepo(Guid owner, params MaintenanceRecordAttachment[] items)
        {
            _owner = owner;
            _items = items.ToList();
        }

        public Guid? NoOwnerFor { get; set; }

        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetPendingStorageMigrationAsync(int take) =>
            Task.FromResult<IReadOnlyList<MaintenanceRecordAttachment>>(_items.Where(a => a.StorageKey is null && a.DataUrl != "").Take(take).ToList());

        public Task<Guid?> GetOwnerIdAsync(Guid attachmentId) =>
            Task.FromResult<Guid?>(attachmentId == NoOwnerFor ? null : _owner);

        public Task SaveChangesAsync() => Task.CompletedTask;

        public Task<MaintenanceRecordAttachment?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdAsync(Guid recordId) => throw new NotImplementedException();
        public Task<IReadOnlyList<MaintenanceRecordAttachment>> GetByRecordIdsAsync(IEnumerable<Guid> recordIds) => throw new NotImplementedException();
        public Task<long> GetTotalSizeBytesByUserAsync(Guid userId) => throw new NotImplementedException();
        public Task<IReadOnlyList<string>> GetStorageKeysByUserAsync(Guid userId) => throw new NotImplementedException();
        public Task AddAsync(MaintenanceRecordAttachment entity) => throw new NotImplementedException();
        public Task RemoveAsync(MaintenanceRecordAttachment entity) => throw new NotImplementedException();
    }
}
