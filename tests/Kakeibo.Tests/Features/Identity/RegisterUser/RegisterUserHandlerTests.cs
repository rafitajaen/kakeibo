using Kakeibo.Api.Features.Identity.Events;
using Kakeibo.Api.Features.Identity.RegisterUser;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Identity.RegisterUser;

public sealed class RegisterUserHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesUserAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var handler = new RegisterUserHandler(db, eventBus, emailService, _clock);

        var result = await handler.HandleAsync(
            new RegisterUserEndpoint.RegisterUserRequest("alice@example.com", "Test1234!", "EUR"), ct);

        var inDb = result.IsSuccess
            ? await db.Users.FirstOrDefaultAsync(u => u.Id == result.Value.Id, ct)
            : null;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("alice@example.com", result.Value.Email),
            () => Assert.NotNull(inDb),
            () => Assert.Equal("alice@example.com", inDb!.Email),
            () => Assert.False(inDb!.IsVerified),
            () => Assert.NotNull(inDb!.EmailVerificationToken));

        eventBus.Received(1).Publish(Arg.Is<UserRegisteredEvent>(e =>
            e.UserId == result.Value.Id && e.Email == "alice@example.com"));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsConflictError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new RegisterUserHandler(
            db, Substitute.For<IEventBus>(), Substitute.For<IEmailService>(), _clock);

        // Seed first registration
        await handler.HandleAsync(
            new RegisterUserEndpoint.RegisterUserRequest("bob@example.com", "Test1234!", "EUR"), ct);

        // Attempt to register again with the same email (different case → still normalized to lowercase)
        var result = await handler.HandleAsync(
            new RegisterUserEndpoint.RegisterUserRequest("BOB@example.com", "AnotherPass9!", "USD"), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }
}
