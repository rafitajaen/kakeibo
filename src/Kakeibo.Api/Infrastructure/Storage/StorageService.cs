using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Kakeibo.Api.Infrastructure.Storage;

// S3-compatible storage service using the MinIO SDK (works with RustFS).
public sealed class StorageService(IOptions<StorageOptions> options, ILogger<StorageService> logger) : IStorageService
{
    private readonly IMinioClient _minioClient = new MinioClient()
        .WithEndpoint(options.Value.Endpoint)
        .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
        .WithSSL(options.Value.UseSSL)
        .Build();

    public async Task<string> UploadFileAsync(
        string bucketName,
        string objectName,
        Stream data,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(data.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        StorageServiceLogs.FileUploaded(logger, bucketName, objectName);

        return $"{bucketName}/{objectName}";
    }

    public async Task<Stream> DownloadFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

        StorageServiceLogs.FileDeleted(logger, bucketName, objectName);
    }

    public async Task<bool> FileExistsAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
            StorageServiceLogs.BucketCreated(logger, bucketName);
        }
    }
}
