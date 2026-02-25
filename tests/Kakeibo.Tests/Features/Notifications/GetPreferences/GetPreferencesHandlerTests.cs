using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.GetPreferences;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Notifications.GetPreferences;

public sealed class GetPreferencesHandlerTests
{
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

    [Fact]
    public async Task ReturnsDefaults_WhenNoPreferencesExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new GetPreferencesHandler(db);
        var result = await handler.HandleAsync(user.Id, ct);

        // All defaults are true
        Assert.True(result.EmailBudgetAlerts);
        Assert.True(result.EmailGoalMilestones);
        Assert.True(result.EmailInvitations);
        Assert.True(result.EmailRecurringUpdates);
        Assert.True(result.PushBudgetAlerts);
        Assert.True(result.PushGoalMilestones);
        Assert.True(result.PushInvitations);
        Assert.True(result.PushRecurringUpdates);
    }

    [Fact]
    public async Task CreatesDefaultRow_InDatabase_WhenNoPreferencesExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new GetPreferencesHandler(db);
        await handler.HandleAsync(user.Id, ct);

        // Verify upsert-on-read: a row was persisted
        var prefs = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        Assert.NotNull(prefs);
    }

    [Fact]
    public async Task ReturnsExistingPreferences_WhenRowExists()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        // Pre-create preferences with some disabled
        db.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = user.Id,
            EmailBudgetAlerts = false,
            PushGoalMilestones = false
        });
        await db.SaveChangesAsync(ct);

        var handler = new GetPreferencesHandler(db);
        var result = await handler.HandleAsync(user.Id, ct);

        Assert.False(result.EmailBudgetAlerts);
        Assert.False(result.PushGoalMilestones);
        // Others still true
        Assert.True(result.EmailGoalMilestones);
        Assert.True(result.PushBudgetAlerts);
    }

    [Fact]
    public async Task IsIdempotent_CallingTwice_ReturnsSameResult()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new GetPreferencesHandler(db);
        var first = await handler.HandleAsync(user.Id, ct);
        var second = await handler.HandleAsync(user.Id, ct);

        Assert.Equal(first.EmailBudgetAlerts, second.EmailBudgetAlerts);
        Assert.Equal(first.PushBudgetAlerts, second.PushBudgetAlerts);

        // Only one row should exist (not duplicated)
        var count = await db.NotificationPreferences
            .CountAsync(p => p.UserId == user.Id, ct);
        Assert.Equal(1, count);
    }
}
