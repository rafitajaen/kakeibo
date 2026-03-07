using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Transactions.Categories.ArchiveCategory;
using Kakeibo.Api.Features.Transactions.Categories.CreateCategory;
using Kakeibo.Api.Features.Transactions.Categories.ListCategories;
using Kakeibo.Api.Features.Transactions.Categories.UpdateCategory;

namespace Kakeibo.Tests.Features.Transactions.Categories;

/// <summary>
/// Integration tests for the Categories domain.
/// Verifies CRUD lifecycle, system category protection, isolation between users,
/// and seed data correctness using a real PostgreSQL database.
/// </summary>
public sealed class CategoryHandlerTests
{
    private static readonly FakeClock TestClock =
        new(Instant.FromUtc(2026, 3, 1, 12, 0));

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
    public async Task SystemCategories_SeedData_TwelveSystemCategoriesExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new ListCategoriesHandler(db);

        // Create a user so we have a valid userId context.
        var user = await CreateUserAsync(db);
        var result = await handler.HandleAsync(user.Id, includeArchived: false, ct);

        var systemCategories = result.Where(c => c.IsSystem).ToList();
        Assert.Equal(12, systemCategories.Count);

        // Verify expected category names are present.
        var names = systemCategories.Select(c => c.Name).ToHashSet();
        Assert.Contains("Housing", names);
        Assert.Contains("Transportation", names);
        Assert.Contains("Food & Dining", names);
        Assert.Contains("Health & Wellness", names);
        Assert.Contains("Entertainment & Leisure", names);
        Assert.Contains("Shopping & Personal", names);
        Assert.Contains("Education", names);
        Assert.Contains("Subscriptions & Bills", names);
        Assert.Contains("Savings & Investments", names);
        Assert.Contains("Debt & Loans", names);
        Assert.Contains("Gifts & Donations", names);
        Assert.Contains("Other", names);
    }

    [Fact]
    public async Task SystemCategories_SeedData_HaveColorAndIconDefaults()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new ListCategoriesHandler(db);

        var result = await handler.HandleAsync(user.Id, includeArchived: false, ct);

        // All 12 system categories must have icon, background color, and text color seeded.
        var systemCategories = result.Where(c => c.IsSystem).ToList();
        foreach (var cat in systemCategories)
        {
            Assert.False(string.IsNullOrEmpty(cat.Icon),            $"Category '{cat.Name}' is missing Icon.");
            Assert.False(string.IsNullOrEmpty(cat.BackgroundColor), $"Category '{cat.Name}' is missing BackgroundColor.");
            Assert.False(string.IsNullOrEmpty(cat.TextColor),       $"Category '{cat.Name}' is missing TextColor.");
        }

        // Spot-check Housing category defaults.
        var housing = systemCategories.First(c => c.Name == "Housing");
        Assert.Equal("Home",     housing.Icon);
        Assert.Equal("#EFF6FF",  housing.BackgroundColor);
        Assert.Equal("#1D4ED8",  housing.TextColor);
    }

    [Fact]
    public async Task ListCategories_ReturnsSystemAndUserCustom_ExcludesOtherUsersCategories()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);

        // UserA creates a custom category.
        await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("UserA Custom"), userA.Id, ct);

        // UserB creates a custom category.
        await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("UserB Custom"), userB.Id, ct);

        var listHandler = new ListCategoriesHandler(db);

        // UserA should see 12 system + 1 custom (not UserB's).
        var resultA = await listHandler.HandleAsync(userA.Id, includeArchived: false, ct);
        Assert.Equal(13, resultA.Count);
        Assert.Contains(resultA, c => c.Name == "UserA Custom" && !c.IsSystem);
        Assert.DoesNotContain(resultA, c => c.Name == "UserB Custom");

        // UserB should see 12 system + 1 custom (not UserA's).
        var resultB = await listHandler.HandleAsync(userB.Id, includeArchived: false, ct);
        Assert.Equal(13, resultB.Count);
        Assert.Contains(resultB, c => c.Name == "UserB Custom" && !c.IsSystem);
        Assert.DoesNotContain(resultB, c => c.Name == "UserA Custom");
    }

    [Fact]
    public async Task CreateCategory_ValidName_CreatesCustomCategory()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateCategoryHandler(db);

        var result = await handler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hobbies", result.Value.Name);
        Assert.False(result.Value.IsSystem);
        Assert.False(result.Value.IsArchived);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ConflictsWithSystemCategory_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateCategoryHandler(db);

        // System category "Housing" already exists.
        var result = await handler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Housing"), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ConflictsWithOwnCustom_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateCategoryHandler(db);

        await handler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        var duplicate = await handler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        Assert.True(duplicate.IsFailure);
        Assert.Equal("conflict", duplicate.Error.Code);
    }

    [Fact]
    public async Task UpdateCategory_ValidRename_UpdatesName()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);
        var updateHandler = new UpdateCategoryHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        var updated = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateCategoryEndpoint.UpdateCategoryRequest("Recreation"),
            user.Id, ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Recreation", updated.Value.Name);
    }

    [Fact]
    public async Task UpdateCategory_SystemCategory_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new UpdateCategoryHandler(db, TestClock);

        // "Housing" is a system category with a fixed GUID.
        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var result = await handler.HandleAsync(
            housingId,
            new UpdateCategoryEndpoint.UpdateCategoryRequest("Renamed Housing"),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCategory_OtherUsersCategory_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);
        var updateHandler = new UpdateCategoryHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("UserA Category"), userA.Id, ct);

        // UserB tries to rename UserA's category.
        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateCategoryEndpoint.UpdateCategoryRequest("Hijacked"),
            userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ArchiveCategory_SystemCategory_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new ArchiveCategoryHandler(db, TestClock);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var result = await handler.HandleAsync(housingId, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ArchiveCategory_OwnCustomCategory_ArchivesSuccessfully()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);
        var archiveHandler = new ArchiveCategoryHandler(db, TestClock);
        var listHandler = new ListCategoriesHandler(db);

        var created = await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        var archiveResult = await archiveHandler.HandleAsync(created.Value.Id, user.Id, ct);
        Assert.True(archiveResult.IsSuccess);

        // Not visible when includeArchived=false.
        var activeList = await listHandler.HandleAsync(user.Id, includeArchived: false, ct);
        Assert.DoesNotContain(activeList, c => c.Name == "Hobbies");

        // Visible when includeArchived=true.
        var allList = await listHandler.HandleAsync(user.Id, includeArchived: true, ct);
        var archived = allList.FirstOrDefault(c => c.Name == "Hobbies");
        Assert.NotNull(archived);
        Assert.True(archived.IsArchived);
    }

    [Fact]
    public async Task ArchiveCategory_AlreadyArchived_IsIdempotent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);
        var archiveHandler = new ArchiveCategoryHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("Hobbies"), user.Id, ct);

        // Archive once.
        await archiveHandler.HandleAsync(created.Value.Id, user.Id, ct);

        // Archive again — must succeed (idempotent).
        var secondArchive = await archiveHandler.HandleAsync(created.Value.Id, user.Id, ct);
        Assert.True(secondArchive.IsSuccess);
    }

    [Fact]
    public async Task ListCategories_SystemCategoriesAppearFirst()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateCategoryHandler(db);
        var listHandler = new ListCategoriesHandler(db);

        // Create a custom category whose name sorts before system categories.
        await createHandler.HandleAsync(
            new CreateCategoryEndpoint.CreateCategoryRequest("AAA Custom"), user.Id, ct);

        var result = await listHandler.HandleAsync(user.Id, includeArchived: false, ct);

        // System categories must all come before custom categories.
        var systemEnd = result.ToList().FindLastIndex(c => c.IsSystem);
        var customStart = result.ToList().FindIndex(c => !c.IsSystem);

        Assert.True(systemEnd < customStart, "All system categories must appear before any custom category.");
    }
}
