# Phase 1c: Events System

**Status**: ✅ Complete
**Objective**: Implement the in-memory async event bus for fire-and-forget event delivery

---

## Prerequisites

| Item | Status |
|------|--------|
| Infrastructure Base | ✅ Phase 1a |
| Identity Backend | ✅ Phase 1b |
| `IEvent`, `IEventBus`, `IEventHandler<T>` interfaces | ✅ Phase 1a |
| `AppDbContext` registered | ✅ Phase 1a |

**Rationale**: The event system needs Identity implemented first to have real events (`UserRegisteredEvent`, `UserLoggedInEvent`) for meaningful end-to-end testing.

---

## Scope

### ✅ Included

- `ChannelEventBus` — singleton `IEventBus` implementation using `System.Threading.Channels`
- `EventDispatcher` — `BackgroundService` that reads from the channel and dispatches to handlers
- DI registration: `ChannelEventBus` as singleton `IEventBus`, `EventDispatcher` as hosted service
- Scrutor scan: auto-registers all `IEventHandler<T>` implementations from the assembly
- Architecture test: types under `Infrastructure/Events/` follow naming conventions

### ❌ Excluded

- Persistent event store
- Event replay / backfill
- Distributed message brokers
- Guaranteed delivery across process restarts

---

## Deliverables

### New Files

**`Kakeibo.Api/Infrastructure/Events/`**:
```
ChannelEventBus.cs     — Singleton: writes to Channel<IEvent> in Publish()
EventDispatcher.cs     — BackgroundService: reads Channel<IEvent>, resolves IEventHandler<T> via DI
```

### Modified Files

**`Program.cs`**:
- Register `ChannelEventBus` as singleton `IEventBus`
- Register `EventDispatcher` as hosted service
- Scrutor scan registers all `IEventHandler<T>` implementations with scoped lifetime

---

## Technical Detail

### Event Flow

```
Feature handler
  → eventBus.Publish(new SomeEvent {...})       [non-blocking, returns void]
  → ChannelEventBus writes to Channel<IEvent>

EventDispatcher (background)
  → reads IEvent from channel
  → creates DI scope
  → resolves all IEventHandler<SomeEvent> from scope
  → invokes each handler sequentially
  → if no handler registered: event discarded silently
```

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| `void Publish()` (fire-and-forget) | Caller never blocks. Handler failures don't affect the originating HTTP request. |
| `System.Threading.Channels` | Lightweight, built-in, no external broker dependency. |
| `BackgroundService` dispatcher | Handlers run outside the HTTP request lifecycle. |
| No persistent store | MVP does not need guaranteed delivery across restarts. |
| Scrutor scan for handlers | Auto-registers all `IEventHandler<T>` without explicit registration. |
| No registered handler → discard | Handlers for Notifications and Auditing are added in later phases and pick up events automatically once registered. |

---

## Acceptance Criteria

- [x] `ChannelEventBus` (singleton) writes events to `Channel<IEvent>` in `Publish()`
- [x] `EventDispatcher` (BackgroundService) reads channel and dispatches to handlers
- [x] All `IEventHandler<T>` implementations auto-registered via Scrutor with scoped lifetime
- [x] Publishing an event with no registered handler discards it silently (no exception)
- [x] `EventDispatcher` creates a new DI scope per event to support scoped services in handlers
- [x] Architecture tests: types under `Infrastructure/Events/` follow naming conventions
- [x] DI wired correctly in Program.cs

---

## Definition of "Phase 1c Completed"

1. All interfaces and implementations exist
2. All acceptance criteria checked (7 items)
3. `EventDispatcher` runs as background service without errors
4. Phase 1d can begin (Audit handlers register as `IEventHandler<T>`)

---

**Next Sub-Phase:** [Phase 1d: Audit Logging](./phase-1d.md)
