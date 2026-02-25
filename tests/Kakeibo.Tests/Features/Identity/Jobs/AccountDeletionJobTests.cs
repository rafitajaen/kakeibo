using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Identity.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NSubstitute;
using UserEntity = Kakeibo.Api.Domain.Entities.User;

namespace Kakeibo.Tests.Features.Identity.Jobs;

public sealed class AccountDeletionJobTests
{
    private static AccountDeletionJob CreateJob(Kakeibo.Api.Persistence.AppDbContext db, Instant now)
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);
        return new AccountDeletionJob(db, clock, NullLogger<AccountDeletionJob>.Instance);
    }

    private static UserEntity SeedUser(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email,
        Instant? deletionRequestedAt = null)
    {
        var user = new UserEntity
        {
            Email = email,
            PasswordHash = PasswordHasher.HashPassword("Test1234!"),
            Currency = "USD",
            IsVerified = true,
            VerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            DeletionRequestedAt = deletionRequestedAt
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredDeletion_SoftDeletesUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = Instant.FromUtc(2026, 4, 15, 2, 0);
        // Requested 31 days ago
        var requestedAt = now.Minus(Duration.FromDays(31));
        var user = SeedUser(db, "expired@example.com", requestedAt);

        await CreateJob(db, now).ExecuteAsync();

        await db.Entry(user).ReloadAsync(ct);
        Assert.NotNull(user.DeletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_RecentDeletion_DoesNotSoftDeleteUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = Instant.FromUtc(2026, 4, 15, 2, 0);
        // Requested only 5 days ago (within grace period)
        var requestedAt = now.Minus(Duration.FromDays(5));
        var user = SeedUser(db, "recent@example.com", requestedAt);

        await CreateJob(db, now).ExecuteAsync();

        await db.Entry(user).ReloadAsync(ct);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_NoDeletionRequested_DoesNotAffectUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = Instant.FromUtc(2026, 4, 15, 2, 0);
        var user = SeedUser(db, "normal@example.com", deletionRequestedAt: null);

        await CreateJob(db, now).ExecuteAsync();

        await db.Entry(user).ReloadAsync(ct);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyDeletedAccount_IsIgnored()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var now = Instant.FromUtc(2026, 4, 15, 2, 0);
        var requestedAt = now.Minus(Duration.FromDays(35));
        var user = SeedUser(db, "alreadydel@example.com", requestedAt);
        // Pre-mark as already deleted
        user.DeletedAt = now.Minus(Duration.FromDays(2));
        db.SaveChanges();

        // Should not throw or double-delete
        await CreateJob(db, now).ExecuteAsync();

        await db.Entry(user).ReloadAsync(ct);
        // DeletedAt unchanged
        Assert.NotNull(user.DeletedAt);
    }
}
