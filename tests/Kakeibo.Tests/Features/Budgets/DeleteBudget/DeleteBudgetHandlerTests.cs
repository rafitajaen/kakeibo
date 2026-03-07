using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.DeleteBudget;
using Kakeibo.Api.Features.Budgets.ListBudgets;

namespace Kakeibo.Tests.Features.Budgets.DeleteBudget;

public sealed class DeleteBudgetHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");

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

    [Fact]
    public async Task ValidRequest_SoftDeletesBudget()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var deleteHandler = new DeleteBudgetHandler(db, TestClock);
        var listHandler = new ListBudgetsHandler(db);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        var result = await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);

        Assert.True(result.IsSuccess);

        // Verify soft-deleted — not in active list
        var budgets = await listHandler.HandleAsync(user.Id, null, null, ct);
        Assert.Empty(budgets);
    }

    [Fact]
    public async Task NotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new DeleteBudgetHandler(db, TestClock);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task OtherUsersBudget_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var deleteHandler = new DeleteBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            userA.Id, ct);

        var result = await deleteHandler.HandleAsync(created.Value.Id, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task AlreadyDeleted_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var deleteHandler = new DeleteBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);

        // Second delete should return not found (already soft-deleted)
        var result = await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }
}
