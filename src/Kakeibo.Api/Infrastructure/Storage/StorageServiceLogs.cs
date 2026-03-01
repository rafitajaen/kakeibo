namespace Kakeibo.Api.Infrastructure.Storage;

internal static partial class StorageServiceLogs
{
    [LoggerMessage(1301, LogLevel.Information,
        "File uploaded to bucket {BucketName} with name {ObjectName}")]
    internal static partial void FileUploaded(this ILogger logger, string bucketName, string objectName);

    [LoggerMessage(1302, LogLevel.Information,
        "File deleted from bucket {BucketName} with name {ObjectName}")]
    internal static partial void FileDeleted(this ILogger logger, string bucketName, string objectName);

    [LoggerMessage(1303, LogLevel.Information,
        "Bucket {BucketName} created")]
    internal static partial void BucketCreated(this ILogger logger, string bucketName);
}
