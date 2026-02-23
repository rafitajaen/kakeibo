namespace Kakeibo.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);
}
