# Friendship Lifecycle

Complete documentation of the friendship system covering request/accept flow, bidirectional normalization, deletion impact, and events.

---

## Friend Request Flow

### Send Request

```
POST /api/friends/requests { receiverUserId }
```

1. Validate sender ≠ receiver (no self-friendship)
2. Verify receiver user exists
3. Check not already friends (normalized lookup)
4. Check no pending request in either direction
5. Create `FriendRequest` (pending state)
6. Publish `FriendRequestSentEvent`

### Accept Request

```
POST /api/friends/requests/{id}/accept
```

1. Find pending request by ID
2. Verify caller is the **receiver** (sender cannot accept own request)
3. Check not already accepted or rejected
4. Set `AcceptedAt = now`
5. Create `Friendship` with normalized IDs (smaller GUID = UserAId)
6. Publish `FriendRequestAcceptedEvent`

### Reject Request

```
POST /api/friends/requests/{id}/reject
```

1. Find pending request by ID
2. Verify caller is the **receiver**
3. Check not already accepted or rejected
4. Set `RejectedAt = now`
5. Publish `FriendRequestRejectedEvent`

### Cancel Request

```
DELETE /api/friends/requests/{id}
```

1. Find pending request by ID
2. Verify caller is the **sender** (only sender can cancel)
3. Check not already accepted or rejected
4. Soft delete (set `DeletedAt`)

---

## Friendship Normalization

To prevent duplicate friendships (A→B and B→A), the `Friendship` entity always stores IDs in normalized order:

```
UserAId = min(userId1, userId2)
UserBId = max(userId1, userId2)
```

A unique index on `(UserAId, UserBId)` enforces this at the database level.

**Lookup pattern:** When checking if two users are friends:
```csharp
var (userAId, userBId) = a.CompareTo(b) < 0 ? (a, b) : (b, a);
var friendship = await db.Friendships
    .FirstOrDefaultAsync(f => f.UserAId == userAId && f.UserBId == userBId);
```

**Listing friends:** Requires checking both sides:
```csharp
var asA = db.Friendships.Where(f => f.UserAId == userId).Select(f => f.UserB);
var asB = db.Friendships.Where(f => f.UserBId == userId).Select(f => f.UserA);
var friends = asA.Concat(asB);
```

---

## API Endpoints

| Operation | Route | Method | Description |
|-----------|-------|--------|-------------|
| SendFriendRequest | `/api/friends/requests` | POST | Send request (by userId) |
| ListReceivedRequests | `/api/friends/requests` | GET | Pending received requests |
| ListSentRequests | `/api/friends/requests/sent` | GET | Pending sent requests |
| AcceptFriendRequest | `/api/friends/requests/{id}/accept` | POST | Accept → create Friendship |
| RejectFriendRequest | `/api/friends/requests/{id}/reject` | POST | Reject request |
| CancelFriendRequest | `/api/friends/requests/{id}` | DELETE | Cancel own sent request |
| ListFriends | `/api/friends` | GET | List current friends |
| DeleteFriendship | `/api/friends/{id}` | DELETE | Remove friendship |
| CheckFriendshipImpact | `/api/friends/{id}/impact` | GET | Shared wallets affected |
| SearchUsers | `/api/users/search?q={query}` | GET | Partial username search (max 20) |
| GetUserProfile | `/api/users/{id}/profile` | GET | Public profile with friendship status |

---

## Events

| Event | Payload | Consumers |
|-------|---------|-----------|
| `FriendRequestSentEvent` | RequestId, SenderUserId, ReceiverUserId | Notifications |
| `FriendRequestAcceptedEvent` | RequestId, SenderUserId, ReceiverUserId, FriendshipId | Notifications, Auditing |
| `FriendRequestRejectedEvent` | RequestId, SenderUserId, ReceiverUserId | Auditing |
| `FriendshipDeletedEvent` | FriendshipId, UserAId, UserBId, DeletedByUserId | Notifications, Auditing, Wallets (Phase E) |

---

## Entity Design

### Friendship

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid (Entity base) | Primary key |
| UserAId | Guid | Smaller GUID (normalized) |
| UserBId | Guid | Larger GUID (normalized) |
| CreatedAt | Instant (Entity base) | Friendship start date |
| DeletedAt | Instant? (Entity base) | Soft delete |

Unique index: `(UserAId, UserBId)`

### FriendRequest

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid (Entity base) | Primary key |
| SenderUserId | Guid | Who sent the request |
| ReceiverUserId | Guid | Who received the request |
| AcceptedAt | Instant? | When accepted (null = not accepted) |
| RejectedAt | Instant? | When rejected (null = not rejected) |
| CreatedAt | Instant (Entity base) | When sent |

Unique index: `(SenderUserId, ReceiverUserId)`

Computed properties (EF Core ignored):
- `IsPending => AcceptedAt is null && RejectedAt is null`
- `IsAccepted => AcceptedAt is not null`
- `IsRejected => RejectedAt is not null`

---

## Logging

EventId range: 3200–3299 (in `FriendshipLogs.cs`)

| EventId | Level | Description |
|---------|-------|-------------|
| 3200 | Info | Friend request sent |
| 3201 | Info | Friend request accepted |
| 3202 | Info | Friend request rejected |
| 3203 | Info | Friend request cancelled |
| 3204 | Info | Friendship deleted |

---

## Deletion Impact (Phase E)

When a friendship is deleted between User X and User Y:
1. Find all shared wallets where both are members
2. Non-owners lose access (WalletMember removed)
3. Owners keep their wallet
4. Guest access to personal wallets revoked

The `CheckFriendshipImpact` endpoint previews this before deletion.
