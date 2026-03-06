using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Infrastructure.Storage;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.UploadAvatar;

// Uploads a user avatar to RustFS and stores the public URL in the user profile.
public sealed class UploadAvatarHandler(AppDbContext db, IStorageService storage, IClock clock)
{
    private static readonly long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<Result<UploadAvatarEndpoint.UploadAvatarResponse>> HandleAsync(
        IFormFile file,
        Guid userId,
        CancellationToken ct)
    {
        // Validate file type and size before touching storage
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return Error.Validation("Avatar must be a JPEG, PNG, or WebP image.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return Error.Validation("Avatar file must be 5 MB or smaller.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        // Use a deterministic object name so re-uploads overwrite the previous avatar.
        var extension = file.ContentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "bin"
        };
        var objectName = $"{userId}/avatar.{extension}";

        await storage.EnsureBucketExistsAsync(BucketNames.Avatars, ct);

        using var stream = file.OpenReadStream();
        var avatarUrl = await storage.UploadFileAsync(BucketNames.Avatars, objectName, stream, file.ContentType, ct);

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        return new UploadAvatarEndpoint.UploadAvatarResponse(avatarUrl);
    }
}
