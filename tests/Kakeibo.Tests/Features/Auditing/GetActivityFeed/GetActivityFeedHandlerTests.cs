using Kakeibo.Api.Features.Auditing.GetActivityFeed;
using Kakeibo.Api.Infrastructure.Audit;
using NodaTime;

namespace Kakeibo.Tests.Features.Auditing.GetActivityFeed;

public sealed class GetActivityFeedHandlerTests
{
    private static ActivityEntry MakeEntry(string action = "transaction.recorded", string? entityType = "Transaction") =>
        new(
            Id: Guid.NewGuid(),
            Action: action,
            EntityType: entityType,
            EntityId: Guid.NewGuid(),
            OccurredAt: SystemClock.Instance.GetCurrentInstant());

    // Wraps entries into the tuple shape expected by IAuditService.GetActivitiesAsync.
    private static (IReadOnlyList<ActivityEntry>, int) EmptyResult() =>
        (Array.Empty<ActivityEntry>(), 0);

    private static (IReadOnlyList<ActivityEntry>, int) Result(params ActivityEntry[] entries) =>
        (entries, entries.Length);

    [Fact]
    public async Task ReturnsEmptyFeed_WhenNoActivitiesForUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        var result = await handler.HandleAsync(Guid.NewGuid(), null, null, null, 1, 20, ct);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task ReturnsActivities_FromAuditService()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var entries = new[]
        {
            MakeEntry("wallet.created"),
            MakeEntry("transaction.recorded")
        };

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            userId, Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Result(entries));

        var handler = new GetActivityFeedHandler(auditService);
        var result = await handler.HandleAsync(userId, null, null, null, 1, 20, ct);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task PassesStartDate_ToAuditService()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var startDate = new LocalDate(2026, 3, 1);

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        await handler.HandleAsync(userId, startDate, null, null, 1, 20, ct);

        await auditService.Received(1).GetActivitiesAsync(
            userId, startDate, null, null, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PassesEndDate_ToAuditService()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var endDate = new LocalDate(2026, 3, 31);

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        await handler.HandleAsync(userId, null, endDate, null, 1, 20, ct);

        await auditService.Received(1).GetActivitiesAsync(
            userId, null, endDate, null, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PassesActionType_ToAuditService()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        const string actionType = "transaction.recorded";

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        await handler.HandleAsync(userId, null, null, actionType, 1, 20, ct);

        await auditService.Received(1).GetActivitiesAsync(
            userId, null, null, actionType, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaginatesCorrectly_WithOffsetCalculation()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);

        // Page 2 with pageSize 10 → offset = (2-1)*10 = 10
        await handler.HandleAsync(userId, null, null, null, 2, 10, ct);

        await auditService.Received(1).GetActivitiesAsync(
            userId, null, null, null, 10, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsPage1_WhenPageIsLessThanOne()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        var result = await handler.HandleAsync(userId, null, null, null, 0, 20, ct);

        // Page clamped to 1 → offset = 0
        await auditService.Received(1).GetActivitiesAsync(
            userId, null, null, null, 0, 20, Arg.Any<CancellationToken>());
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task ClampsPageSize_ToMaximumOf100()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(EmptyResult());

        var handler = new GetActivityFeedHandler(auditService);
        var result = await handler.HandleAsync(userId, null, null, null, 1, 500, ct);

        // pageSize 500 should be clamped to 100
        await auditService.Received(1).GetActivitiesAsync(
            userId, null, null, null, 0, 100, Arg.Any<CancellationToken>());
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task MapsActivityEntries_ToResponseItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var occurredAt = SystemClock.Instance.GetCurrentInstant();
        var entry = new ActivityEntry(Guid.NewGuid(), "wallet.created", "Wallet", entityId, occurredAt);

        var auditService = Substitute.For<IAuditService>();
        auditService.GetActivitiesAsync(
            Arg.Any<Guid>(), Arg.Any<LocalDate?>(), Arg.Any<LocalDate?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((new[] { entry }, 1));

        var handler = new GetActivityFeedHandler(auditService);
        var result = await handler.HandleAsync(userId, null, null, null, 1, 20, ct);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal(entry.Id, item.Id);
        Assert.Equal("wallet.created", item.Action);
        Assert.Equal("Wallet", item.EntityType);
        Assert.Equal(entityId, item.EntityId);
    }
}
