# Edge Case Catalog

Complete catalog of edge cases to cover across all test levels. Use this as a checklist
when implementing a new feature or reviewing test coverage.

---

## ValueObject

- Two instances with the same components → equal (`==`, `Equals`, `GetHashCode` identical)
- One component different → not equal (`!=`, `!Equals`, hash code different)
- `null` vs empty string in a component → not equal (they are semantically distinct)
- `GetHashCode` consistent across multiple calls on the same instance (deterministic)
- Null component in equality comparison → does not throw; compares correctly against non-null
- `ValueObject` used as Dictionary/HashSet key → hash-based lookup finds the correct entry

---

## Middleware

### ErrorHandlingMiddleware

- `ArgumentNullException` → 400 Bad Request (client error)
- `UnauthorizedAccessException` → 401 Unauthorized
- Generic `Exception` → 500 Internal Server Error
- Error response body is valid JSON (not an HTML error page)
- Error response includes a `traceId` field for log correlation
- No exception thrown → pipeline continues, status code unchanged (200 unless handler sets it)

### AuditContextMiddleware

- `X-Forwarded-For` header present → `AuditContextAccessor.IpAddress` set to header value
- `X-Forwarded-For` absent → `IpAddress` falls back to `RemoteIpAddress`
- `User-Agent` header → `AuditContextAccessor.UserAgent` set correctly
- Authenticated request with `sub` claim → `ActorId` set to parsed `Guid`
- Anonymous request (no `sub` claim) → `ActorId` is `null`
- Invalid `sub` claim (not a GUID) → `ActorId` is `null`, no exception

### JwtRevocationMiddleware

- JTI present in Redis deny-list → 401 Unauthorized, pipeline stops (next() not called)
- JTI not in deny-list → pipeline continues (next() called)
- No `jti` claim in JWT → Redis key check skipped entirely (anonymous or malformed token passes through)
- Redis unavailable → behavior depends on fail-open vs fail-closed policy (must be explicit)
- Expired key in Redis → Redis handles TTL, middleware behavior unchanged

---

## Infrastructure

### OutboxInterceptor Atomicity

- Entity save + outbox insert happen in the same DB transaction (atomic)
- If commit fails → neither entity nor outbox row exists (no orphaned messages)
- Domain events list on entity is empty after `SaveChangesAsync` (prevents re-dispatch)
- Multiple events buffered before `SaveChangesAsync` → all are persisted in a single batch

### OutboxProcessor

- Unprocessed message → consumer called + `ProcessedAt` set after successful dispatch
- Already-processed message → consumer NOT called (idempotent polling)
- Consumer throws → message remains unprocessed (`ProcessedAt` null), will be retried on next poll
- Same message dispatched twice (simulated retry) → consumer called once (second poll skips it)
- Empty outbox → no consumer calls, no errors

**How to verify "consumer throws → message stays unprocessed"** (Level 2c / infrastructure test):

```csharp
[Fact]
public async Task ConsumeAsync_Throws_MessageRemainsUnprocessed()
{
    // Arrange: seed an outbox message directly
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;

    var message = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = nameof(UserRegisteredEvent),
        Payload = JsonSerializer.Serialize(BuildEvent(), DefaultSerializer.Options),
        CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
        ProcessedAt = null,  // unprocessed
    };
    db.OutboxMessages.Add(message);
    await db.SaveChangesAsync(ct);

    // Consumer that always throws
    var consumer = Substitute.For<IEventConsumer<UserRegisteredEvent>>();
    consumer
        .ConsumeAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>())
        .ThrowsAsync(new InvalidOperationException("simulated failure"));

    var processor = new OutboxProcessor(db, consumer, NullLogger<OutboxProcessor>.Instance);

    // Act: process one batch — the consumer throws, processor catches it
    await processor.ProcessBatchAsync(ct);

    // Assert: message NOT marked as processed (available for retry)
    var inDb = await db.OutboxMessages.FindAsync([message.Id], ct);
    Assert.NotNull(inDb);
    Assert.Null(inDb.ProcessedAt);  // still null — not yet successfully dispatched
}
```

### AuditOutboxProcessor

- `Type == "AuditEventEnvelope"` messages → mapped to `AuditRow` and written to ClickHouse
- Non-audit messages (regular integration events) → skipped by the audit processor
- `Action`, `Module`, `EntityType`, `EntityId`, `ActorId` fields mapped correctly to `AuditRow`
- Multiple envelopes → all batched in a single `WriteBulkAsync` call + all marked processed

### DomainEventHandlers — No DbContext injection

Domain event handlers (`IDomainEventHandler<T>`) are dispatched by `OutboxInterceptor` during
`SaveChangesAsync` — they run inside the same DB transaction. Injecting a `DbContext` into a
domain event handler creates a re-entrancy risk and breaks the handler's single responsibility
(publish events + stage audit). This architectural constraint must be enforced by an architecture test.

**Architecture test:**

```csharp
[Fact]
public void DomainEventHandlers_ShouldNotInjectDbContext()
{
    var handlerInterface = typeof(IDomainEventHandler<>);

    // Find all concrete IDomainEventHandler<T> implementations
    var handlerTypes = SourceAssemblies
        .SelectMany(a => a.GetTypes())
        .Where(t => !t.IsAbstract && !t.IsInterface
            && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface))
        .ToList();

    // Check every constructor for DbContext parameters
    var offending = handlerTypes
        .Where(t => t.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => typeof(DbContext).IsAssignableFrom(p.ParameterType)))
        .Select(t => t.FullName)
        .ToList();

    Assert.Empty(offending);
    // If this fails: move DbContext usage to the feature handler or consumer,
    // not in the domain event handler.
}
```

**How to spot the violation in code review:** any constructor parameter of a type that ends in
`DbContext` inside a class ending in `DomainEventHandler` is a violation.

### PermissionService

- Role = `SuperAdmin` → `HasPermissionAsync` returns `true` for any permission without querying the DB
- Permissions loaded on first call → cached in L1 cache (in-process)
- Second call for same user → L1 cache hit, no additional DB query
- Permission exists in role → returns `true` (case-sensitive match)
- Permission does NOT exist in role → returns `false`
- Mixed-case permission string → does NOT match lowercase stored value

---

## Auth & Security

- Access token expired → automatic transparent refresh (user stays on page)
- Refresh token expired → redirect to login, session terminated
- Invalid JWT signature → 401 (not 500)
- Endpoint with required permission missing → 403 with specific error code
- Endpoint without authentication → 401
- SuperAdmin role: cannot be deleted (Rule 1 — returns `400 Role.SuperAdminProtected`)
- SuperAdmin role: cannot be renamed (Rule 1)
- SuperAdmin role: permissions cannot be modified (Rule 1)
- Last SuperAdmin: cannot be deleted (Rule 2 — returns `400 User.LastSuperAdmin`)
- Last SuperAdmin: role cannot be changed (Rule 2)
- Simultaneous login attempts with the same credentials (race condition)
- Password reset token: expired → descriptive error
- Password reset token: already used → descriptive error
- Login with unverified email → specific error, not generic 401
- Account locked (too many failed attempts) → 423 or 401 with lockout message
- Token refresh while another refresh is in-flight (concurrent refresh race)

---

## Database & Persistence

- Soft delete: `IsDeleted` query filter hides records from ALL queries including update and delete
- Uniqueness: duplicate email/username → Conflict error, never `DbUpdateException` bubbling up
- Foreign key: create child entity with non-existent parent ID → controlled error response
- Concurrent writes: two requests create the same unique entity simultaneously → only one persists
- NodaTime `Instant` stored and recovered without timezone drift
- `Guid7` ordering: time-ordered IDs are actually ordered when queried by `ORDER BY id`
- Large dataset: pagination prevents memory overflow (verify with realistic row counts)
- Soft-deleted entity: cannot be retrieved via normal queries (filter enforced)
- Soft-deleted entity: can be retrieved when query explicitly includes deleted records (admin use case)
- Update on non-existent ID → NotFound error, not a silent no-op
- Optimistic concurrency: two concurrent updates to the same entity → second update detects conflict

---

## Outbox & Idempotence

- At-least-once delivery: consumer receives the same event twice → result is idempotent (no duplicate data)
- `OutboxInterceptor`: entity save + outbox insert are atomic (if the commit fails, no orphaned outbox messages)
- Consumer throws exception: message remains unprocessed (not marked as processed, will be retried)
- Consumer succeeds: message marked as processed
- Retry exhaustion: after 3 retries with exponential backoff (1s, 5s, 15s), message marked as failed
- OutboxProcessor disabled in tests: background service removed from `WebApplicationFactory`
- Manual outbox processing: for integration tests that need to verify consumer behavior, trigger `OutboxProcessor` explicitly

---

## Validation & Types

- Exact limit: `MaximumLength(100)` → 100 chars is valid, 101 chars is invalid
- Nullable optional field: null is valid; empty string may not be (verify per field)
- Whitespace: `"   "` is not a valid non-empty string (unless explicitly allowed)
- Invalid enum value in request body → 400 validation error
- Negative numbers where only positive values are allowed
- Zero where only positive values are allowed (vs. zero being valid)
- Numeric overflow: values beyond `decimal.MaxValue` or `int.MaxValue`
- Date in the past where only future dates are valid
- Date in the future where only past dates are valid (e.g., date of birth)
- End date before start date
- Required field missing from JSON body → 400, not 500
- Extra unknown fields in JSON body → ignored, not error (or error if strict mode)
- Malformed JSON body → 400 Bad Request, not 500

---

## External Services

- `INotificationService` unavailable → domain event handler does NOT throw, audit is still staged
- SMTP failure → email not sent, the primary operation continues (non-critical path)
- SMTP failure → `INotificationService` returns `NotificationResult.Failure(...)` (no exception)
- RustFS unavailable → upload returns controlled error, not an unhandled exception
- RustFS upload: large file → no timeout (proper streaming)
- ClickHouse unavailable → audit staging continues (writes queued), application still functions
- External payment gateway timeout → user-facing error, charge not duplicated

---

## Pagination & Lists

- Empty list: 0 items → `200 OK` with empty array (NOT 404)
- Single item: 1 item → returns correctly without off-by-one errors
- Full page: exactly `pageSize` items → returns `pageSize` items, `hasNextPage = true`
- Last page: fewer than `pageSize` items → returns remaining items, `hasNextPage = false`
- Out-of-range page: page 999 of a 3-page list → empty array (NOT 404)
- Invalid sort field → 400 validation error
- Sort direction: ascending vs descending produce different orderings
- Filtering: combined filters reduce the result set correctly
- Filtering: no results matching filters → empty array (NOT 404)

---

## Domain Business Rules

- Promotion at exactly `MaxTotalUses`: one more use → `Promotion.MaxUsesReached` error
- Promotion exactly at expiry boundary: expired 1 second ago → error; valid 1 second from now → success
- Subscription cancellation: already cancelled → `Subscription.AlreadyCancelled` error
- Booking overlap: reserving a resource already booked → `Reservation.Conflict` error
- Booking in the past → `Reservation.PastDate` error
- Booking beyond max advance window → `Reservation.TooFarInAdvance` error
- Member without active subscription attempting a member-only action → `Membership.Required` error
- Staff scheduling gap: assigning a shift that overlaps with existing shift → error

---

## Frontend — UI States

- **Loading**: skeleton/spinner visible while request is in-flight
- **Error**: error message visible when API returns an error
- **Empty**: empty state component visible when list is empty
- **Success**: success toast/message visible after successful action
- **Malformed API data**: component does not crash if an expected field arrives as null
- **Optimistic update**: UI updates before API confirmation; rolls back if API fails
- **Form submitting**: submit button disabled while form is submitting (prevents double-submit)
- **Form reset**: form clears after successful submission (if expected)
- **Network error in form**: error message shown, form data preserved (user doesn't lose input)
- **Concurrent requests**: multiple rapid clicks on the same button only trigger one request

---

## Mobile — Capacitor Specifics

- Offline → online: pending operations execute when connection restores
- App goes to background → returns to foreground: access token still valid OR refresh is triggered
- Refresh token in Preferences: persists after app restart
- Logout: Preferences cleared, store reset, navigation to login
- Camera permission denied: user-facing error message, no crash
- Camera permission denied once then granted: retry works
- File system permission denied: error handled gracefully
- Large image upload: handled with streaming, no OOM crash
- Network change event: `Network.addListener` cleanup on component unmount
- App killed mid-operation: state is recoverable on next launch

---

## E2E — Network & Environment

- Slow network (Playwright throttling): loading states appear, no premature timeout errors
- API down (502/503): error banner visible, retry button functional
- Session timeout during use: automatic refresh, user remains on current page
- Long-running operation: progress indicator visible, no white screen
- Browser back button during multi-step flow: state is preserved or gracefully reset
- Deep link with invalid/expired token: friendly error page, not a crash
- Two browser tabs with the same session: token refresh in one tab doesn't break the other

---

## Snapshot Regression (Verify)

These scenarios cause snapshot diffs and require scrubbing or accepting the new baseline:

- API response adds a new optional field → snapshot diff catches it (review before accepting)
- API response removes a field → snapshot diff catches it (may indicate a breaking change)
- Email template renames a CSS class → snapshot diff catches structural changes
- Email template changes copy text → snapshot diff catches it
- `NodaTime Instant` serializes differently across timezones → use `InstantConverter` scrubber
- GUID in response body → scrub with `ScrubGuid()` or `ScrubMember("id")`
- `createdAt` / `updatedAt` timestamps in response → scrub with `ScrubMember("createdAt")`
- Pagination metadata (`total`, `page`, `pageSize`) changes → verify intentional
- Enum value renamed in the response → snapshot diff catches it (breaking API change)

See [snapshot-testing.md](snapshot-testing.md) for scrubbing patterns and the acceptance workflow.

---

## E2E — Critical User Flows (must always have end-to-end tests)

- Registration → email verification → first login
- Login → access protected resource → logout
- Password reset: request → receive email → reset → login with new password
- Admin creates member → member appears in list
- Member subscription: subscribe → active → cancel → expired
- Padel court booking: check availability → book → see booking → cancel
- Role management: SuperAdmin cannot be deleted or renamed
