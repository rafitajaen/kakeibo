using Kakeibo.Api.Features.Identity.ImportData;
using Microsoft.Data.Sqlite;

namespace Kakeibo.Tests.Features.Identity.ImportData;

public sealed class OneMoneyImporterTests
{
    private static readonly Guid UserId = Kakeibo.Api.Common.Utils.Guid7.NewGuid();
    private static readonly Instant Now = Instant.FromUtc(2026, 3, 1, 12, 0);
    private static readonly FakeClock Clock = new(Now);

    // Builds a minimal OneMoney SQLite backup and returns its bytes.
    private static byte[] BuildOneMoneyBackup(
        int transactionCount = 2,
        bool includePrimaryBook = true)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using var conn = new SqliteConnection($"Data Source={tempFile}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE ba (_id INTEGER PRIMARY KEY, _ty INTEGER, _na TEXT);
                CREATE TABLE de (_id INTEGER PRIMARY KEY, _b_i INTEGER, _ty INTEGER, _na TEXT);
                CREATE TABLE tr (_id INTEGER PRIMARY KEY, _b_i INTEGER, _ty INTEGER, _da INTEGER, _a_m TEXT, _co TEXT, _a_i INTEGER, _d_i INTEGER);
                """;
            cmd.ExecuteNonQuery();

            cmd.CommandText = includePrimaryBook
                ? "INSERT INTO ba VALUES (1, 1, 'Mi libro')"
                : "INSERT INTO ba VALUES (1, 2, 'Backup')";
            cmd.ExecuteNonQuery();

            // Two accounts (_ty=0) and two expense categories (_ty=1)
            cmd.CommandText = """
                INSERT INTO de VALUES (10, 1, 0, 'Cuenta Corriente');
                INSERT INTO de VALUES (11, 1, 0, 'Efectivo');
                INSERT INTO de VALUES (20, 1, 1, 'Alimentación');
                INSERT INTO de VALUES (21, 1, 1, 'Transporte');
                """;
            cmd.ExecuteNonQuery();

            // Unix milliseconds for 2025-01-15 and 2025-01-20 UTC (NodaTime, no DateTimeOffset)
            var date1Ms = Instant.FromUtc(2025, 1, 15, 0, 0).ToUnixTimeMilliseconds();
            var date2Ms = Instant.FromUtc(2025, 1, 20, 0, 0).ToUnixTimeMilliseconds();

            for (var i = 1; i <= transactionCount; i++)
            {
                var dateMs = i % 2 == 0 ? date2Ms : date1Ms;
                var amount = (i * 10.5m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var acct = i % 2 == 0 ? 11 : 10;
                var cat = i % 2 == 0 ? 21 : 20;
                cmd.CommandText = $"INSERT INTO tr VALUES ({i}, 1, 0, {dateMs}, '{amount}', 'Pago {i}', {acct}, {cat})";
                cmd.ExecuteNonQuery();
            }

            conn.Close();
            return File.ReadAllBytes(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Import_ValidOneMoneyBackup_ReturnsCorrectCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new OneMoneyImporter(db, Clock);

        var backupBytes = BuildOneMoneyBackup(transactionCount: 4);
        var result = await importer.ImportAsync(
            new MemoryStream(backupBytes), "money.backup", UserId, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("onemoney", result.Value.Source);
        Assert.Equal("sqlite", result.Value.Format);
        Assert.Equal(2, result.Value.Counts.Wallets);
        Assert.Equal(4, result.Value.Counts.Transactions);
        Assert.True(result.Value.Counts.Categories >= 2);
    }

    [Fact]
    public async Task Import_NoPrimaryBook_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new OneMoneyImporter(db, Clock);

        var backupBytes = BuildOneMoneyBackup(includePrimaryBook: false);
        var result = await importer.ImportAsync(
            new MemoryStream(backupBytes), "money.backup", UserId, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Import_NonSqliteFile_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new OneMoneyImporter(db, Clock);

        var notSqlite = "This is not a SQLite file"u8.ToArray();
        var result = await importer.ImportAsync(
            new MemoryStream(notSqlite), "money.backup", UserId, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Import_CreatesWalletBalances_ForEachImportedWallet()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new OneMoneyImporter(db, Clock);

        var backupBytes = BuildOneMoneyBackup(transactionCount: 2);
        var result = await importer.ImportAsync(
            new MemoryStream(backupBytes), "money.backup", UserId, ct);

        Assert.True(result.IsSuccess);

        var ctx2 = TestDbContextFactory.CreateSecondContext(db);
        var walletCount = ctx2.Wallets.Count(w => w.OwnerId == UserId);
        var balanceCount = ctx2.WalletBalances.Count();
        Assert.Equal(walletCount, balanceCount);
    }

    [Fact]
    public async Task Import_TransactionDate_ParsedCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = await TestDbContextFactory.CreateAsync();
        var importer = new OneMoneyImporter(db, Clock);

        var backupBytes = BuildOneMoneyBackup(transactionCount: 1);
        var result = await importer.ImportAsync(
            new MemoryStream(backupBytes), "money.backup", UserId, ct);

        Assert.True(result.IsSuccess);

        var ctx2 = TestDbContextFactory.CreateSecondContext(db);
        var tx = ctx2.Transactions.First(t => t.UserId == UserId);

        // Instant.FromUtc(2025, 1, 15, 0, 0) in UTC → LocalDate 2025-01-15
        Assert.Equal(new LocalDate(2025, 1, 15), tx.Date);
    }
}
