using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.DeleteAttachment;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kakeibo.Tests.Features.Transactions.DeleteAttachment;

public sealed class DeleteAttachmentHandlerTests
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
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 1000m });
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

    // Seeds an attachment record in the DB and returns its ID.
    private static async Task<Guid> SeedAttachmentAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid transactionId, Guid userId)
    {
        var attachmentId = Kakeibo.Api.Common.Utils.Guid7.NewGuid();
        db.TransactionAttachments.Add(new TransactionAttachment
        {
            Id = attachmentId,
            TransactionId = transactionId,
            FileName = "receipt.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            ObjectName = $"{transactionId}/{attachmentId}/receipt.jpg",
            UploadedByUserId = userId
        });
        await db.SaveChangesAsync();
        return attachmentId;
    }

    private static DeleteAttachmentHandler MakeHandler(
        Kakeibo.Api.Persistence.AppDbContext db,
        IStorageService? storage = null)
    {
        return new DeleteAttachmentHandler(
            db,
            storage ?? Substitute.For<IStorageService>(),
            NullLogger<DeleteAttachmentHandler>.Instance);
    }

    [Fact]
    public async Task AttachmentNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = MakeHandler(db);

        var result = await handler.HandleAsync(txId, Guid.NewGuid(), user.Id, ct);

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
        var attachmentId = await SeedAttachmentAsync(db, txId, userA.Id);
        var handler = MakeHandler(db);

        // userB has no access to the wallet
        var result = await handler.HandleAsync(txId, attachmentId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ValidDelete_RemovesAttachmentFromDb()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var attachmentId = await SeedAttachmentAsync(db, txId, user.Id);

        var storage = Substitute.For<IStorageService>();
        var handler = MakeHandler(db, storage);

        var result = await handler.HandleAsync(txId, attachmentId, user.Id, ct);

        Assert.True(result.IsSuccess);
        // Verify storage delete was called
        await storage.Received(1).DeleteFileAsync(
            Kakeibo.Api.Common.Utils.BucketNames.Attachments,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        // Verify record removed from DB
        var remaining = db.TransactionAttachments.Any(a => a.Id == attachmentId);
        Assert.False(remaining);
    }
}
