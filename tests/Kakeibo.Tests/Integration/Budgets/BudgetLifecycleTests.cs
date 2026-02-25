using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.DeleteBudget;
using Kakeibo.Api.Features.Budgets.ListBudgets;
using Kakeibo.Api.Features.Budgets.UpdateBudget;

namespace Kakeibo.Tests.Integration.Budgets;

/// <summary>
/// Integration tests for Budget CRUD lifecycle using a real PostgreSQL database.
/// Verifies end-to-end Create → List → Update → Delete flow and access control isolation.
/// </summary>
public sealed class BudgetLifecycleTests
{
    private static readonly NodaTime.Testing.FakeClock TestClock =
        new(NodaTime.Instant.FromUtc(2026, 3, 1, 12, 0));
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodId = Guid.Parse("10000000-0000-0000-0000-000000000003");

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
    public async Task FullCrudLifecycle_Create_List_Update_Delete()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var createHandler = new CreateBudgetHandler(db);
        var listHandler = new ListBudgetsHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);
        var deleteHandler = new DeleteBudgetHandler(db, TestClock);

        // Create
        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Housing Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        Assert.True(created.IsSuccess);
        Assert.Equal("Housing Budget", created.Value.Name);
        Assert.Equal(0m, created.Value.CurrentSpending);

        // List — should have 1 budget
        var listed = await listHandler.HandleAsync(user.Id, null, null, ct);
        Assert.Single(listed);
        Assert.Equal("Housing Budget", listed[0].Name);

        // Update
        var updated = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Housing Updated", FoodId, 800m, "2026-02-01", "2026-11-30", null),
            user.Id, ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Housing Updated", updated.Value.Name);
        Assert.Equal(FoodId, updated.Value.CategoryId);
        Assert.Equal(800m, updated.Value.Limit);
        Assert.Equal("2026-02-01", updated.Value.StartDate);
        Assert.Equal("2026-11-30", updated.Value.EndDate);

        // List after update — still 1 budget with updated fields
        var listedAfterUpdate = await listHandler.HandleAsync(user.Id, null, null, ct);
        Assert.Single(listedAfterUpdate);
        Assert.Equal("Housing Updated", listedAfterUpdate[0].Name);

        // Delete
        var deleted = await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);
        Assert.True(deleted.IsSuccess);

        // List after delete — should be empty
        var listedAfterDelete = await listHandler.HandleAsync(user.Id, null, null, ct);
        Assert.Empty(listedAfterDelete);
    }

    [Fact]
    public async Task AccessControl_UserB_CannotModifyUserABudget()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);

        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);
        var deleteHandler = new DeleteBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("UserA Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            userA.Id, ct);

        // UserB cannot update UserA's budget
        var updateResult = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Hijacked", HousingId, 999m, "2026-01-01", "2026-12-31", null),
            userB.Id, ct);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("forbidden", updateResult.Error.Code);

        // UserB cannot delete UserA's budget
        var deleteResult = await deleteHandler.HandleAsync(created.Value.Id, userB.Id, ct);
        Assert.True(deleteResult.IsFailure);
        Assert.Equal("forbidden", deleteResult.Error.Code);

        // UserA's budget is still intact
        var listHandler = new ListBudgetsHandler(db);
        var listed = await listHandler.HandleAsync(userA.Id, null, null, ct);
        Assert.Single(listed);
        Assert.Equal("Housing Budget", listed[0].Name);
    }
}
