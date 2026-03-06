using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.ExportData;
using Kakeibo.Api.Features.Identity.ImportData;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kakeibo.Tests.Features.Identity.ImportData;

public sealed class KakeiboImporterTests
{
    private static readonly Guid UserId = Guid7.NewGuid();
    private static readonly Instant Now = Instant.FromUtc(2026, 3, 1, 12, 0);
    private static readonly FakeClock Clock = new(Now);

    // Builds an export SQLite and returns its bytes.
    private static async Task<byte[]> BuildExportBytesAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new ExportDataHandler(db, NullLogger<ExportDataHandler>.Instance);
        var result = await handler.HandleAsync(UserId, "sqlite", ct);
        Assert.True(result.IsSuccess);

        var ms = new MemoryStream();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = ms;
        await result.Value.ExecuteAsync(ctx);
        return ms.ToArray();
    }

    private static async Task SeedMinimalData(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        var walletId = Guid7.NewGuid();
        db.Wallets.Add(new Wallet
        {
            Id = walletId, Name = "Checking", Type = WalletType.Personal,
            OwnerId = UserId, Currency = "EUR", CreatedAt = Now, UpdatedAt = Now
        });
        db.WalletBalances.Add(new WalletBalance { WalletId = walletId, Balance = 500m, UpdatedAt = Now });

        var categoryId = Guid7.NewGuid();
        db.Categories.Add(new Category
        {
            Id = categoryId, Name = "Salary", UserId = UserId, CreatedAt = Now, UpdatedAt = Now
        });
        db.Transactions.Add(new Transaction
        {
            Id = Guid7.NewGuid(), Type = TransactionType.Income, Amount = 500m,
            Description = "Monthly salary", Date = new LocalDate(2026, 3, 1),
            CategoryId = categoryId, WalletId = walletId, UserId = UserId,
            CreatedAt = Now, UpdatedAt = Now
        });
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task RoundTrip_SQLite_ImportedCountsMatchExportedCounts()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var sourceDb = await TestDbContextFactory.CreateAsync();
        await SeedMinimalData(sourceDb);
        var exportBytes = await BuildExportBytesAsync(sourceDb);

        await using var targetDb = await TestDbContextFactory.CreateAsync();
        var importer = new KakeiboImporter(targetDb, new FakeClock(Instant.FromUtc(2026, 3, 1, 13, 0)));
        var result = await importer.ImportAsync(
            new MemoryStream(exportBytes), "kakeibo-export.db", UserId, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Counts.Wallets);
        Assert.Equal(1, result.Value.Counts.Categories);
        Assert.Equal(1, result.Value.Counts.Transactions);
        Assert.Equal("kakeibo", result.Value.Source);
        Assert.Equal("sqlite", result.Value.Format);
    }

    [Fact]
    public async Task Import_NonSqliteFile_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new KakeiboImporter(db, Clock);

        var notSqlite = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        var result = await importer.ImportAsync(
            new MemoryStream(notSqlite), "bad.bin", UserId, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Import_IsAdditive_ExistingDataNotDeleted()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();

        // Pre-seed one wallet in the target DB
        var existingWalletId = Guid7.NewGuid();
        db.Wallets.Add(new Wallet
        {
            Id = existingWalletId, Name = "Pre-existing Wallet", Type = WalletType.Personal,
            OwnerId = UserId, Currency = "EUR", CreatedAt = Now, UpdatedAt = Now
        });
        db.WalletBalances.Add(new WalletBalance { WalletId = existingWalletId, Balance = 0m, UpdatedAt = Now });
        await db.SaveChangesAsync(ct);

        // Build export from a source DB with different data
        await using var sourceDb = await TestDbContextFactory.CreateAsync();
        await SeedMinimalData(sourceDb);
        var exportBytes = await BuildExportBytesAsync(sourceDb);

        var importer = new KakeiboImporter(db, Clock);
        var result = await importer.ImportAsync(
            new MemoryStream(exportBytes), "kakeibo-export.db", UserId, ct);

        Assert.True(result.IsSuccess);

        // Both old and new wallets must exist
        var ctx2 = TestDbContextFactory.CreateSecondContext(db);
        var walletCount = ctx2.Wallets.Count(w => w.OwnerId == UserId);
        Assert.Equal(2, walletCount);
    }
}
