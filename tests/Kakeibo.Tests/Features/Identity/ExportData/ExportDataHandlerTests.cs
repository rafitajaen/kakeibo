using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.ExportData;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Compression;

namespace Kakeibo.Tests.Features.Identity.ExportData;

public sealed class ExportDataHandlerTests
{
    private static readonly Guid UserId = Guid7.NewGuid();
    private static readonly Instant Now = Instant.FromUtc(2026, 3, 1, 12, 0);

    private static async Task SeedMinimalData(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;

        // User must exist in DB because WalletMember has a FK to Users
        var user = new User
        {
            Id = UserId,
            Email = "export-test@example.com",
            PasswordHash = "hash",
            Username = "export_test",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);

        var wallet = new Wallet
        {
            Id = Guid7.NewGuid(), Name = "Test Wallet", Type = WalletType.Personal,
            Currency = "EUR", CreatedAt = Now, UpdatedAt = Now
        };
        db.Wallets.Add(wallet);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = UserId, Role = WalletMemberRole.Owner });
        db.WalletBalances.Add(new WalletBalance
        {
            WalletId = wallet.Id, Balance = 100m, UpdatedAt = Now
        });
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task ExportSqlite_ReturnsNonEmptyStream_WithWalletsTable()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        await SeedMinimalData(db);

        var handler = new ExportDataHandler(db, NullLogger<ExportDataHandler>.Instance);
        var result = await handler.HandleAsync(UserId, "sqlite", ct);

        Assert.True(result.IsSuccess);

        var ms = await ExecuteResultAsync(result.Value);
        Assert.True(ms.Length > 0);

        // Validate SQLite magic bytes
        var magic = new byte[4];
        ms.Read(magic, 0, 4);
        Assert.Equal([0x53, 0x51, 0x4C, 0x69], magic);

        // Validate expected tables exist
        ms.Seek(0, SeekOrigin.Begin);
        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var f = File.OpenWrite(tempFile))
                ms.CopyTo(f);

            using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var tables = new List<string>();
            while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));

            Assert.Contains("wallets", tables);
            Assert.Contains("transactions", tables);
            Assert.Contains("categories", tables);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportCsv_ReturnsZipWithExpectedEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        await SeedMinimalData(db);

        var handler = new ExportDataHandler(db, NullLogger<ExportDataHandler>.Instance);
        var result = await handler.HandleAsync(UserId, "csv", ct);

        Assert.True(result.IsSuccess);

        var ms = await ExecuteResultAsync(result.Value);
        Assert.True(ms.Length > 0);

        // Validate ZIP magic bytes
        var magic = new byte[4];
        ms.Read(magic, 0, 4);
        Assert.Equal([0x50, 0x4B, 0x03, 0x04], magic);

        ms.Seek(0, SeekOrigin.Begin);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var entryNames = archive.Entries.Select(e => e.Name).ToHashSet();

        Assert.Contains("wallets.csv", entryNames);
        Assert.Contains("transactions.csv", entryNames);
        Assert.Contains("categories.csv", entryNames);
        Assert.Contains("wallet_balances.csv", entryNames);
    }

    [Fact]
    public async Task ExportSqlite_WalletRowPresent_WhenUserHasWallet()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        await SeedMinimalData(db);

        var handler = new ExportDataHandler(db, NullLogger<ExportDataHandler>.Instance);
        var result = await handler.HandleAsync(UserId, "sqlite", ct);

        Assert.True(result.IsSuccess);

        var ms = await ExecuteResultAsync(result.Value);
        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var f = File.OpenWrite(tempFile))
                ms.CopyTo(f);

            using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM wallets";
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            Assert.Equal(1L, count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Executes an IResult and returns the response body as a MemoryStream.
    private static async Task<MemoryStream> ExecuteResultAsync(IResult httpResult)
    {
        var ms = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = ms;
        await httpResult.ExecuteAsync(context);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
