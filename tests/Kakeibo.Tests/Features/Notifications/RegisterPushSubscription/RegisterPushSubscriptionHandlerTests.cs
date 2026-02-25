using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.RegisterPushSubscription;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Notifications.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionHandlerTests
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

    private static RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionRequest MakeRequest(
        string? endpoint = null) =>
        new(
            Endpoint: endpoint ?? $"https://push.example.com/{Guid.NewGuid():N}",
            P256dh: "BNcRdreALRFXTkOOUHK1EtK2wtwe6YQLLoINZe76uMTRB5ieKNi8qzoaGPJnP4U8wfFdvQaA74udn-aEYB8AXw==",
            Auth: "tBHItJI5svbpez7KI4CCXg=="
        );

    [Fact]
    public async Task SavesSubscription_WhenNewEndpoint()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var request = MakeRequest();
        var handler = new RegisterPushSubscriptionHandler(db);
        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.NotEqual(Guid.Empty, result.Id);

        var saved = await db.PushSubscriptions
            .FirstOrDefaultAsync(ps => ps.Id == result.Id, ct);

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.UserId);
        Assert.Equal(request.Endpoint, saved.Endpoint);
        Assert.Equal(request.P256dh, saved.P256dh);
        Assert.Equal(request.Auth, saved.Auth);
    }

    [Fact]
    public async Task UpdatesExistingSubscription_WhenEndpointAlreadyExists()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var endpoint = $"https://push.example.com/{Guid.NewGuid():N}";
        var firstRequest = MakeRequest(endpoint);

        var handler = new RegisterPushSubscriptionHandler(db);
        var firstResult = await handler.HandleAsync(firstRequest, user.Id, ct);

        // Re-subscribe with same endpoint but different keys
        var updatedRequest = new RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionRequest(
            Endpoint: endpoint,
            P256dh: "NEW_P256DH_KEY==",
            Auth: "NEW_AUTH_KEY=="
        );
        var secondResult = await handler.HandleAsync(updatedRequest, user.Id, ct);

        // Same subscription ID returned (upsert)
        Assert.Equal(firstResult.Id, secondResult.Id);

        // Keys updated in DB
        var saved = await db.PushSubscriptions
            .FirstOrDefaultAsync(ps => ps.Id == firstResult.Id, ct);

        Assert.Equal("NEW_P256DH_KEY==", saved!.P256dh);
        Assert.Equal("NEW_AUTH_KEY==", saved.Auth);

        // No duplicate row
        var count = await db.PushSubscriptions
            .CountAsync(ps => ps.UserId == user.Id && ps.Endpoint == endpoint, ct);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AllowsMultipleDevices_ForSameUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new RegisterPushSubscriptionHandler(db);
        await handler.HandleAsync(MakeRequest(), user.Id, ct);
        await handler.HandleAsync(MakeRequest(), user.Id, ct);

        var count = await db.PushSubscriptions
            .CountAsync(ps => ps.UserId == user.Id, ct);

        // Two distinct endpoints = two separate subscriptions
        Assert.Equal(2, count);
    }
}
