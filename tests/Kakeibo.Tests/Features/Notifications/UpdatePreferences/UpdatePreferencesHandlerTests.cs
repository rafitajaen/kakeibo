using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.UpdatePreferences;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

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

    private static UpdatePreferencesEndpoint.UpdatePreferencesRequest AllDisabledRequest() =>
        new(false, false, false, false, false, false, false, false);

    private static UpdatePreferencesEndpoint.UpdatePreferencesRequest AllEnabledRequest() =>
        new(true, true, true, true, true, true, true, true);

    [Fact]
    public async Task CreatesPreferences_WhenNoRowExists()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new UpdatePreferencesHandler(db, TestClock);
        await handler.HandleAsync(AllDisabledRequest(), user.Id, ct);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        Assert.NotNull(prefs);
        Assert.False(prefs.EmailBudgetAlerts);
        Assert.False(prefs.PushBudgetAlerts);
    }

    [Fact]
    public async Task UpdatesExistingPreferences_WhenRowAlreadyExists()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        // Create initial preferences (all enabled)
        db.NotificationPreferences.Add(new NotificationPreferences { UserId = user.Id });
        await db.SaveChangesAsync(ct);

        var handler = new UpdatePreferencesHandler(db, TestClock);
        // Update to all disabled
        await handler.HandleAsync(AllDisabledRequest(), user.Id, ct);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        Assert.NotNull(prefs);
        Assert.False(prefs.EmailBudgetAlerts);
        Assert.False(prefs.EmailGoalMilestones);
        Assert.False(prefs.EmailInvitations);
        Assert.False(prefs.EmailRecurringUpdates);
        Assert.False(prefs.PushBudgetAlerts);
        Assert.False(prefs.PushGoalMilestones);
        Assert.False(prefs.PushInvitations);
        Assert.False(prefs.PushRecurringUpdates);
    }

    [Fact]
    public async Task SetsUpdatedAt_ToClockInstant()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new UpdatePreferencesHandler(db, TestClock);
        await handler.HandleAsync(AllEnabledRequest(), user.Id, ct);

        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        Assert.Equal(TestClock.GetCurrentInstant(), prefs!.UpdatedAt);
    }

    [Fact]
    public async Task DoesNotCreateDuplicateRow_WhenCalledTwice()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new UpdatePreferencesHandler(db, TestClock);
        await handler.HandleAsync(AllEnabledRequest(), user.Id, ct);
        await handler.HandleAsync(AllDisabledRequest(), user.Id, ct);

        var count = await db.NotificationPreferences
            .CountAsync(p => p.UserId == user.Id, ct);
        Assert.Equal(1, count);
    }
}
