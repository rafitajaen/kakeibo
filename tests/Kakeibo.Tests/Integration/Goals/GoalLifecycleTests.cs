using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.DeleteGoal;
using Kakeibo.Api.Features.Goals.ListGoals;
using Kakeibo.Api.Features.Goals.UpdateGoal;

namespace Kakeibo.Tests.Integration.Goals;

/// <summary>
/// Integration tests for Goal CRUD lifecycle using a real PostgreSQL database.
/// Verifies end-to-end Create → List → Update → Delete flow and access control isolation.
/// </summary>
public sealed class GoalLifecycleTests
{
    private static readonly NodaTime.Testing.FakeClock TestClock =
        new(NodaTime.Instant.FromUtc(2026, 3, 1, 12, 0));

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

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "Savings")
    {
        var wallet = new Wallet { Name = name, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task FullCrudLifecycle_Create_List_Update_Delete()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, user.Id, "Vacation Fund");
        var walletB = await CreateWalletAsync(db, user.Id, "Emergency Fund");

        var createHandler = new CreateGoalHandler(db, TestClock);
        var listHandler = new ListGoalsHandler(db);
        var updateHandler = new UpdateGoalHandler(db, TestClock);
        var deleteHandler = new DeleteGoalHandler(db, TestClock);

        // Create
        var created = await createHandler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("Europe Vacation", 5000m, "2027-12-31", walletA.Id),
            user.Id, ct);

        Assert.True(created.IsSuccess);
        Assert.Equal("Europe Vacation", created.Value.Name);
        Assert.Equal(5000m, created.Value.TargetAmount);
        Assert.Equal("2027-12-31", created.Value.Deadline);
        Assert.Equal(0m, created.Value.CurrentProgress);
        Assert.Equal(0, created.Value.LastMilestone);

        // List — should have 1 goal
        var listed = await listHandler.HandleAsync(user.Id, null, ct);
        Assert.Single(listed);
        Assert.Equal("Europe Vacation", listed[0].Name);
        Assert.Equal("Vacation Fund", listed[0].WalletName);

        // Update — change name, amount, remove deadline, change wallet
        var updated = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateGoalEndpoint.UpdateGoalRequest("Emergency Fund Goal", 10000m, null, walletB.Id),
            user.Id, ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Emergency Fund Goal", updated.Value.Name);
        Assert.Equal(10000m, updated.Value.TargetAmount);
        Assert.Null(updated.Value.Deadline);
        Assert.Equal("Emergency Fund", updated.Value.WalletName);

        // List after update — still 1 goal with updated name
        var listedAfterUpdate = await listHandler.HandleAsync(user.Id, null, ct);
        Assert.Single(listedAfterUpdate);
        Assert.Equal("Emergency Fund Goal", listedAfterUpdate[0].Name);

        // Delete
        var deleted = await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);
        Assert.True(deleted.IsSuccess);

        // List after delete — should be empty
        var listedAfterDelete = await listHandler.HandleAsync(user.Id, null, ct);
        Assert.Empty(listedAfterDelete);
    }

    [Fact]
    public async Task AccessControl_UserB_CannotModifyUserAGoal()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, userA.Id);

        var createHandler = new CreateGoalHandler(db, TestClock);
        var updateHandler = new UpdateGoalHandler(db, TestClock);
        var deleteHandler = new DeleteGoalHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest("UserA Goal", 5000m, null, wallet.Id),
            userA.Id, ct);

        // UserB cannot update UserA's goal
        var updateResult = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateGoalEndpoint.UpdateGoalRequest("Hijacked", 999m, null, wallet.Id),
            userB.Id, ct);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("forbidden", updateResult.Error.Code);

        // UserB cannot delete UserA's goal
        var deleteResult = await deleteHandler.HandleAsync(created.Value.Id, userB.Id, ct);
        Assert.True(deleteResult.IsFailure);
        Assert.Equal("forbidden", deleteResult.Error.Code);

        // UserA's goal is still intact
        var listHandler = new ListGoalsHandler(db);
        var listed = await listHandler.HandleAsync(userA.Id, null, ct);
        Assert.Single(listed);
        Assert.Equal("UserA Goal", listed[0].Name);
    }
}
