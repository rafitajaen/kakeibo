# 12 — Friends

## Purpose

The Friends section manages the social graph of the app. Friends are used when transferring wallet ownership and when inviting people to shared wallets without knowing their email. The section includes a friend list, user search, friend requests, and a user profile view.

---

## Friends View

The friends view (`/friends`) is the main friends management screen. It has two functional areas: a user search at the top and the friends list below.

**Header area:**

- A **Requests** button (or link) in the top right shows a badge with the count of pending received friend requests. Clicking it navigates to the friend requests view.

**User search:**

- A text input with a search icon. Placeholder: "Search by username..."
- As the user types, the input performs a debounced search (300ms delay) against the API for users matching the entered username.
- Results are shown in a `UserSearchResults` component below the search input. Each result shows the user's avatar (or initials fallback), display name, and username.
- If the found user is already a friend, the result shows a "Friends" label.
- If a friend request is already pending (sent or received), the result shows the appropriate status.
- If the user is eligible to add, a **Send friend request** button appears on the result row. Clicking it sends the request immediately and updates the row to show "Request sent."
- The search results panel is dismissed by clearing the input or clicking outside.

**Friends list:**

- A list of all current friends. Each `FriendCard` shows:
  - Avatar or initials.
  - Display name.
  - Username (prefixed with @).
  - "Friends since" date.
  - **View Profile** button — navigates to that user's profile page.
  - **Remove Friend** button — opens a confirmation dialog (see below).

If the user has no friends yet, an empty state message encourages them to search for friends.

On mount, the view fetches the current friends list and the pending received requests count.

---

## Remove Friend Dialog

When the user clicks **Remove Friend**, the app first calls a backend endpoint to check whether the friendship has any shared wallet impact (i.e., are both users members of the same shared wallet?).

**If there are no shared wallets:** A simple confirmation dialog appears asking "Are you sure you want to remove [name] from your friends?" with Confirm and Cancel buttons.

**If there are shared wallets:** The dialog shows a warning with an expanded impact preview:

- A list of shared wallets both users are members of.
- For each shared wallet, the user's role is shown (e.g., "Your role: Owner" or "Your role: Member").
- Wallets where the user is not the owner show a "You will lose access to this wallet" badge in a destructive red color, because removing the friendship also removes wallet membership.
- Wallets where the user is the owner are listed without the loss-of-access warning (since the owner controls membership).

The user must explicitly confirm removal despite the warnings. Canceling closes the dialog without making any changes.

---

## Friend Requests View

The friend requests view (`/friends/requests`) uses a two-tab interface:

**Received tab** — shows all incoming friend requests. Each `FriendRequestCard` displays:

- The sender's avatar and display name.
- The sender's username.
- **Accept** button — accepts the request. The sender is added to the user's friends list and this request disappears from the received tab. A success toast is shown.
- **Reject** button — rejects the request. The request is dismissed without adding the sender as a friend. A toast confirms the rejection.

A badge next to the "Received" tab label shows the count of pending requests.

**Sent tab** — shows all friend requests sent by the user that are still pending (not yet accepted or rejected). Each `SentRequestCard` displays:

- The recipient's avatar and display name.
- The recipient's username.
- **Cancel** button — cancels the sent request. The request is withdrawn and the recipient will no longer see it.

On mount, both the received and sent request lists are fetched.

---

## User Profile View

The user profile view (`/friends/:userId`) shows a public profile card for another user.

**Displayed information:**

- Avatar (with initials fallback).
- Display name.
- Username (prefixed with @).

**Friendship status indicator:**

- If already friends: shows a "Friends since [date]" label with a UserCheck icon. No add button is shown.
- If a friend request was sent by the current user and is pending: shows a disabled "Request sent" button. No additional actions are available until the recipient responds.
- If a friend request was received from this user: shows Accept and Reject buttons inline on the profile.
- If no relationship exists: shows an **Add friend** button. Clicking it sends a friend request and updates the indicator to "Request sent."

**Actions:**

- **Add friend** — sends a friend request. Button becomes disabled after clicking and label changes to "Request sent."
- **Accept / Reject** — available when a pending received request exists from this user.

On mount, the view fetches the user's public profile data and the current friendship status.
