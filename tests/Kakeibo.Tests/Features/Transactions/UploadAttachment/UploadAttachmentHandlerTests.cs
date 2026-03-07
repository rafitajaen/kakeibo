using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Features.Transactions.UploadAttachment;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kakeibo.Tests.Features.Transactions.UploadAttachment;

public sealed class UploadAttachmentHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private const string ValidDate = "2026-02-15";
    private static readonly Guid HousingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 1000m });
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> RecordTransactionAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, Guid walletId)
    {
        var handler = new RecordTransactionHandler(db, Substitute.For<IEventBus>(), TestClock);
        var result = await handler.HandleAsync(
            new RecordTransactionEndpoint.RecordTransactionRequest(
                "Expense", 50m, "Test", ValidDate, HousingCategoryId, walletId, null),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    // Creates a fake IFormFile suitable for upload tests.
    private static IFormFile MakeFile(string contentType = "image/jpeg", long sizeBytes = 1024)
    {
        var file = Substitute.For<IFormFile>();
        file.ContentType.Returns(contentType);
        file.Length.Returns(sizeBytes);
        file.FileName.Returns("receipt.jpg");
        file.OpenReadStream().Returns(Stream.Null);
        return file;
    }

    private static UploadAttachmentHandler MakeHandler(
        Kakeibo.Api.Persistence.AppDbContext db,
        IStorageService? storage = null)
    {
        var storageSub = storage ?? Substitute.For<IStorageService>();
        storageSub.UploadFileAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs("https://storage/attachments/test.jpg");
        return new UploadAttachmentHandler(db, storageSub, NullLogger<UploadAttachmentHandler>.Instance);
    }

    [Fact]
    public async Task InvalidContentType_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = MakeHandler(db);

        var result = await handler.HandleAsync(txId, MakeFile("text/plain"), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task FileTooLarge_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = MakeHandler(db);

        // 10 MB + 1 byte exceeds the limit
        var oversizedFile = MakeFile("image/jpeg", 10 * 1024 * 1024 + 1);
        var result = await handler.HandleAsync(txId, oversizedFile, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task TransactionNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = MakeHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), MakeFile(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task NoWalletAccess_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);
        var txId = await RecordTransactionAsync(db, userA.Id, wallet.Id);
        var handler = MakeHandler(db);

        // userB has no access to userA's wallet
        var result = await handler.HandleAsync(txId, MakeFile(), userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ValidUpload_ReturnsAttachmentMetadata()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = MakeHandler(db);

        var result = await handler.HandleAsync(txId, MakeFile("image/png", 2048), user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(txId, result.Value.TransactionId);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(2048, result.Value.FileSizeBytes);
        Assert.Equal(user.Id, result.Value.UploadedByUserId);
    }
}
