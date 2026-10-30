using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Options;
using Bikontrol.Infrastructure.Resilience;
using Microsoft.Extensions.Options;

namespace Bikontrol.Infrastructure.Storage
{
    /// <summary>
    /// Private-bucket adapter for any S3-compatible service (Cloudflare R2,
    /// Backblaze B2, MinIO, AWS S3). Transient failures are retried with the same
    /// policy as SMTP; a missing key on read is a normal "not found".
    /// </summary>
    public sealed class S3ObjectStorage : IObjectStorage, IDisposable
    {
        private readonly AmazonS3Client _client;
        private readonly string _bucket;

        public S3ObjectStorage(IOptions<StorageOptions> options)
        {
            var settings = options.Value;
            _bucket = settings.Bucket!;
            _client = new AmazonS3Client(
                new BasicAWSCredentials(settings.AccessKey!, settings.SecretKey!),
                new AmazonS3Config
                {
                    ServiceURL = settings.ServiceUrl,
                    ForcePathStyle = settings.ForcePathStyle,
                    AuthenticationRegion = settings.Region,
                    // R2 and several S3 clones reject the SDK's default CRC checksum headers.
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                    Timeout = TimeSpan.FromSeconds(15),
                    MaxErrorRetry = 0
                });
        }

        public bool IsEnabled => true;

        public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
        {
            await TransientRetry.ExecuteAsync(async ct =>
            {
                using var stream = new MemoryStream(content, writable: false);
                await _client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = false
                }, ct);
                return true;
            }, IsTransient, cancellationToken: cancellationToken);
        }

        public async Task<StoredObject?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await TransientRetry.ExecuteAsync(
                    ct => _client.GetObjectAsync(_bucket, key, ct), IsTransient, cancellationToken: cancellationToken);
                return new StoredObject(response.ResponseStream, response.Headers.ContentType, response.ContentLength);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task DeleteAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            // DeleteObjects takes up to 1000 keys per call.
            foreach (var batch in keys.Distinct().Chunk(1000))
            {
                await TransientRetry.ExecuteAsync(async ct =>
                {
                    await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _bucket,
                        Objects = batch.Select(k => new KeyVersion { Key = k }).ToList(),
                        Quiet = true
                    }, ct);
                    return true;
                }, IsTransient, cancellationToken: cancellationToken);
            }
        }

        /// <summary>5xx, throttling and network errors are worth another try; 4xx (auth, bad bucket) are not.</summary>
        public static bool IsTransient(Exception exception) => exception switch
        {
            AmazonS3Exception s3 => (int)s3.StatusCode >= 500 || s3.StatusCode == System.Net.HttpStatusCode.TooManyRequests,
            AmazonServiceException service => (int)service.StatusCode >= 500,
            HttpRequestException => true,
            IOException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false
        };

        public void Dispose() => _client.Dispose();
    }
}
