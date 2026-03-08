# Testing Entity Configurations

Entity configurations define how domain entities are mapped to the database: table and column names, constraints (max length, uniqueness, not-null), cascade behaviors, and global query filters (e.g., soft delete). Testing these ensures the schema enforces your business rules at the database level.

---

## What Is an Entity Configuration?

An entity configuration is a class that ends with `Configuration` and implements `IEntityTypeConfiguration<T>`. EF Core discovers and applies all configurations automatically when `ApplyConfigurationsFromAssembly` is called in `AppDbContext.OnModelCreating`.

**Location:** `src/Kakeibo.Api/Persistence/Configurations/{Entity}Configuration.cs`

**Example:**

```csharp
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(254);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
```

---

## Why Test Entity Configurations?

EF Core translates your configuration into SQL DDL. A misconfiguration silently succeeds at compile time but fails at runtime. Tests catch:

- A field accepts strings longer than `HasMaxLength` allows.
- A unique index does not prevent duplicates.
- A soft-deleted entity is still returned by queries because the global filter is broken.
- A foreign key allows orphans because the cascade delete was forgotten.

---

## How to Test Entity Configurations

Use a real PostgreSQL database via `TestDbContextFactory.CreateAsync()`. The schema is applied automatically via `EnsureCreatedAsync`. Then insert and query entities to verify the constraints.

---

## Testing Global Query Filters (Soft Delete)

The soft delete filter (`builder.HasQueryFilter(u => u.DeletedAt == null)`) means that entities with a non-null `DeletedAt` are excluded from all normal queries. Test that it works.

```csharp
[Fact]
public async Task SoftDeletedUser_IsExcludedFromNormalQueries()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;

    // Insert a soft-deleted user
    var user = new User
    {
        Id = Guid7.NewGuid().ToGuid(),
        Email = "deleted@example.com",
        Username = "ghost",
        PasswordHash = "hash",
        Currency = "EUR",
        DeletedAt = SystemClock.Instance.GetCurrentInstant()    // soft-deleted
    };
    db.Users.Add(user);
    await db.SaveChangesAsync(ct);

    // Normal query — filter applied — user should NOT appear
    var found = await db.Users.FirstOrDefaultAsync(u => u.Email == "deleted@example.com", ct);
    Assert.Null(found);

    // Query with filter ignored — user SHOULD appear
    var foundIgnored = await db.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Email == "deleted@example.com", ct);
    Assert.NotNull(foundIgnored);
}
```

---

## Testing Unique Indexes

When a unique index is violated, EF Core throws a `DbUpdateException`. Catch it to assert the constraint is in place.

```csharp
[Fact]
public async Task InsertDuplicateEmail_ThrowsDbUpdateException()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;

    var user1 = new User
    {
        Id = Guid7.NewGuid().ToGuid(),
        Email = "alice@example.com",
        Username = "alice",
        PasswordHash = "hash",
        Currency = "EUR"
    };
    var user2 = new User
    {
        Id = Guid7.NewGuid().ToGuid(),
        Email = "alice@example.com",    // same email — violates unique index
        Username = "alice2",
        PasswordHash = "hash",
        Currency = "EUR"
    };

    db.Users.AddRange(user1, user2);

    await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
}
```

---

## Testing MaxLength Constraints

Attempting to insert a string that exceeds the database column's character limit throws a `DbUpdateException`.

```csharp
[Fact]
public async Task InsertEmailOver254Chars_ThrowsDbUpdateException()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;

    var user = new User
    {
        Id = Guid7.NewGuid().ToGuid(),
        Email = new string('a', 250) + "@x.co",    // 256 chars — exceeds limit of 254
        Username = "alice",
        PasswordHash = "hash",
        Currency = "EUR"
    };
    db.Users.Add(user);

    await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
}
```

> **Note:** EF Core's `HasMaxLength` alone does not throw in C# — the string fits in memory. The exception only occurs when EF Core sends the INSERT to the database and PostgreSQL rejects it. This is why real Testcontainers tests are required here.

---

## Testing Cascade Behaviors

When a parent entity is deleted, the cascade behavior determines what happens to child entities. Test that soft-delete cascades work as expected.

```csharp
[Fact]
public async Task DeleteWallet_CascadesToWalletBalance()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;

    var user = SeedUser(db);
    var wallet = new Wallet { Id = Guid7.NewGuid().ToGuid(), UserId = user.Id, /* ... */ };
    var balance = new WalletBalance { WalletId = wallet.Id, Balance = 100m };
    db.Wallets.Add(wallet);
    db.WalletBalances.Add(balance);
    await db.SaveChangesAsync(ct);

    // Remove the wallet
    db.Wallets.Remove(wallet);
    await db.SaveChangesAsync(ct);

    // The balance should have been cascade-deleted
    var orphanBalance = await db.WalletBalances
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(b => b.WalletId == wallet.Id, ct);
    Assert.Null(orphanBalance);
}
```

---

## What NOT to Test

Some configurations do not need tests because they are enforced at the C# level before reaching the database:

| Configuration | Reason to skip |
|---------------|---------------|
| `HasKey` | EF Core throws before hitting the DB if no key is set |
| `ToTable` / `HasColumnName` | Naming only — no constraint to violate |
| `HasConversion<string>` for enums | Enum validity enforced in C# |
| Navigation properties | Tested implicitly by handler tests that traverse relations |

Focus configuration tests on constraints that PostgreSQL enforces: `NOT NULL`, `UNIQUE`, `MAXLENGTH`, `FOREIGN KEY` cascade, and global query filters.

---

## Complete Example — UserConfiguration Tests

```csharp
public sealed class UserConfigurationTests
{
    [Fact]
    public async Task SoftDeletedUser_ExcludedFromNormalQueries()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var user = new User
        {
            Id = Guid7.NewGuid().ToGuid(),
            Email = "ghost@example.com",
            Username = "ghost",
            PasswordHash = "hash",
            Currency = "EUR",
            DeletedAt = SystemClock.Instance.GetCurrentInstant()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var result = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        Assert.Null(result);
    }

    [Fact]
    public async Task DuplicateEmail_ThrowsDbUpdateException()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        db.Users.AddRange(
            new User { Id = Guid7.NewGuid().ToGuid(), Email = "dup@x.com", Username = "a", PasswordHash = "h", Currency = "EUR" },
            new User { Id = Guid7.NewGuid().ToGuid(), Email = "dup@x.com", Username = "b", PasswordHash = "h", Currency = "EUR" }
        );

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task EmailExactly254Chars_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var user = new User
        {
            Id = Guid7.NewGuid().ToGuid(),
            Email = new string('a', 248) + "@x.com",    // 254 chars — exactly at limit
            Username = "alice",
            PasswordHash = "hash",
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);    // Should NOT throw

        var saved = await db.Users.IgnoreQueryFilters().FindAsync([user.Id], ct);
        Assert.NotNull(saved);
    }
}
```

---

## Checklist Before Submitting Entity Configuration Tests

- [ ] Soft delete global filter tested: deleted entity excluded from normal queries, visible with `IgnoreQueryFilters()`
- [ ] All unique indexes tested: duplicate insert throws `DbUpdateException`
- [ ] Critical `MaxLength` constraints tested at the boundary (exactly at limit succeeds, one over fails)
- [ ] Cascade deletes tested where relevant
- [ ] `Assert.ThrowsAsync<DbUpdateException>()` used for constraint violation tests
- [ ] Test names follow `{Entity}_{Scenario}_{ExpectedResult}` convention
- [ ] Docker skip guard inherited from `TestDbContextFactory`
