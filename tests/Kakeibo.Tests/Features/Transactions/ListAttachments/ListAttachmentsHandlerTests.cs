using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.ListAttachments;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Transactions.ListAttachments;

public sealed class ListAttachmentsHandlerTests
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

    [Fact]
    public async Task TransactionNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new ListAttachmentsHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

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
        var handler = new ListAttachmentsHandler(db);

        var result = await handler.HandleAsync(txId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task NoAttachments_ReturnsEmptyList()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);
        var handler = new ListAttachmentsHandler(db);

        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task WithAttachments_ReturnsItems()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var txId = await RecordTransactionAsync(db, user.Id, wallet.Id);

        // Seed two attachments directly
        db.TransactionAttachments.Add(new Kakeibo.Api.Domain.Entities.TransactionAttachment
        {
            TransactionId = txId,
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 4096,
            ObjectName = $"{txId}/{Guid.NewGuid()}/invoice.pdf",
            UploadedByUserId = user.Id
        });
        db.TransactionAttachments.Add(new Kakeibo.Api.Domain.Entities.TransactionAttachment
        {
            TransactionId = txId,
            FileName = "receipt.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            ObjectName = $"{txId}/{Guid.NewGuid()}/receipt.jpg",
            UploadedByUserId = user.Id
        });
        await db.SaveChangesAsync(ct);

        var handler = new ListAttachmentsHandler(db);
        var result = await handler.HandleAsync(txId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(txId, item.TransactionId));
    }
}
