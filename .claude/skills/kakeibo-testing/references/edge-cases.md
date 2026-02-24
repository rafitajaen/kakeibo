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

### ChannelEventBus

- `Publish` is fire-and-forget — must return immediately without awaiting any handler
- Multiple events published in a loop → all are eventually dequeued by `EventDispatcher`
- Events are not dropped when the channel is at capacity (channel is unbounded by default)
- `Publish` called from a scoped request context → event dispatched in a fresh DI scope

### EventDispatcher

- Handler throws exception → `EventDispatcher` catches it, logs it, continues with next event
- Multiple handlers for the same event type → all are dispatched (one throw does not skip others)
- Handler completes successfully → no exception propagated to `EventDispatcher`
- Concurrent events → `EventDispatcher` processes them in-order from the channel

**How to verify "handler throws → EventDispatcher continues"** (infrastructure test):

```csharp
[Fact]
public async Task EventDispatcher_WhenHandlerThrows_ContinuesProcessingNextEvent()
{
    var ct = TestContext.Current.CancellationToken;
    var successfulEvents = new ConcurrentBag<IEvent>();

    // Build a minimal DI container with a throwing handler and a capturing handler
    var services = new ServiceCollection();
    services.AddSingleton<ChannelEventBus>();
    services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<ChannelEventBus>());
    services.AddHostedService<EventDispatcher>();
    services.AddScoped<IEventHandler<WalletCreatedEvent>, ThrowingEventHandler>();
    services.AddScoped<IEventHandler<TransactionRecordedEvent>>(
        _ => new CapturingEventHandler<TransactionRecordedEvent>(successfulEvents));
    services.AddLogging();

    await using var host = services.BuildServiceProvider();
    foreach (var svc in host.GetServices<IHostedService>())
        await svc.StartAsync(ct);

    var bus = host.GetRequiredService<IEventBus>();
    bus.Publish(new WalletCreatedEvent { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), WalletId = Guid7.NewGuid() });
    bus.Publish(new TransactionRecordedEvent { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), TransactionId = Guid7.NewGuid() });

    await Task.Delay(500, ct);

    // TransactionRecordedEvent must still have been dispatched
    Assert.Single(successfulEvents);

    foreach (var svc in host.GetServices<IHostedService>())
        await svc.StopAsync(ct);
}
```

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

## Events & Idempotence

- Fire-and-forget delivery: handler receives the same event twice (if EventDispatcher retries) → result is idempotent
- `IEventBus.Publish`: fire-and-forget — the feature handler never awaits handler completion
- Event handler throws exception: `EventDispatcher` catches it, logs it; event is NOT re-queued
- Event handler succeeds: no exception propagated back to the publisher
- `EventDispatcher` runs as `BackgroundService`: let it process events with a short `Task.Delay` in integration tests
- Idempotency in handlers: handlers that write to DB must handle duplicate event calls gracefully

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
