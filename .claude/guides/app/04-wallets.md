# 04 — Wallets

## Purpose

The Wallets section allows users to create and manage personal and shared financial containers. It also covers shared wallet membership, invitation management, visibility controls, and wallet transfers.

---

## Wallets List View

The wallets list view (`/wallets`) is the entry point for the Wallets section. It displays a three-tab interface:

- **Personal** — shows the user's personal wallets.
- **Shared** — shows shared wallets the user is a member of.
- **Archived** — shows wallets the user has archived (hidden from the main view).

Each tab renders a `WalletList` component containing `WalletCard` items. Each card shows the wallet's name, current balance (formatted with the user's currency), type badge, and optional custom icon and colors.

Clicking a wallet card navigates to the wallet detail view. A context menu on each card provides quick access to Edit and Archive actions without navigating away.

A **New wallet** button at the top right navigates to the wallet creation form.

---

## Create Wallet

The create wallet view (`/wallets/new`) displays a centered card with a `WalletForm` in creation mode.

**Fields:**

- **Name** — text input. Required. Max 100 characters. Must be unique per user.
- **Type** — select dropdown. Required. Options: Personal, Shared. Determines whether the wallet can have multiple members. Cannot be changed after creation.
- **Initial Balance** — number input. Optional but defaulting to 0. Range: 0 to 999,999,999.99. This field is only shown in create mode; it is hidden in edit mode because the balance is derived from transactions after the first entry.
- **Appearance (collapsible section):**
  - **Icon** — an icon picker showing a searchable grid of lucide icons. Optional. Selecting an icon sets a visual identifier for the wallet card.
  - **Background color** — a color picker (HTML5 native). Optional. Default is `#3B82F6` (blue).
  - **Text color** — a color picker. Optional. Default is `#FFFFFF` (white).
  - A live preview card shows how the wallet will look with the selected icon and colors.

**Actions:**

- **Create wallet** — submits the form. On success, navigates to the new wallet's detail view.
- **Cancel** — navigates back to the wallets list without saving.

**Error handling:**

- HTTP 409: displays "A wallet with this name already exists."
- Validation errors appear inline below each field.

---

## Edit Wallet

The edit wallet view (`/wallets/:id/edit`) is identical to the create form except:

- The **Type** field is disabled and shown as read-only (wallet type cannot be changed after creation).
- The **Initial Balance** field is hidden.
- The form is pre-filled with the current wallet's data.

Saving navigates back to the wallet's detail view. If the name conflicts with another wallet, an inline error is shown.

---

## Wallet Detail View

The wallet detail view (`/wallets/:id`) shows a single wallet's full information and available actions.

**Displayed information:**

- Wallet name as the page heading.
- Current balance displayed prominently in large text with the currency symbol, using tabular-num font to prevent layout shifts on digit changes.
- Wallet type badge (Personal / Shared).

**Visibility selector** (owners only for personal wallets, all members for shared): A dropdown that sets the wallet's visibility:

- **Private** — the wallet and its transactions are not visible to users outside the shared wallet.
- **Public** — the wallet can be found by friends when they search.

The visibility selector is only shown to the wallet owner for personal wallets. For shared wallets, all members can see the setting but only the owner can change it.

**Button group:**

- **View transactions** — navigates to the transactions view pre-filtered to show only this wallet's transactions.
- **Record transaction** — navigates to the transaction creation form with this wallet pre-selected.
- **Edit** — navigates to the edit wallet view.
- **Members** (shared wallets only) — navigates to the shared wallet member management view.
- **Transfer wallet** (personal wallets only, owner only) — opens a transfer dialog (see below).
- **Make private** (shared wallets only, owner only) — opens a make-private confirmation dialog (see below).
- **Archive / Unarchive** — archives or unarchives the wallet. Archived wallets are moved to the Archived tab and excluded from the dashboard. A confirmation dialog appears before archiving.
- **Delete** — permanently deletes the wallet and all its transactions. A destructive confirmation dialog with explicit warning text appears. This action is irreversible.

---

## Transfer Wallet Dialog

The transfer dialog allows the wallet owner to transfer ownership of a personal wallet to a friend. This is useful when onboarding a shared expense scenario or reassigning financial tracking to another person.

**Dialog content:**

- Explanation text: "Transfer this wallet to a friend. They will become the new owner."
- **Friend selector** — a dropdown listing the user's current friends. Required.

**Actions:**

- **Transfer** — confirms the transfer. The wallet is reassigned to the selected friend. The current user loses ownership and the wallet disappears from their wallet list.
- **Cancel** — closes the dialog without transferring.

---

## Make Private Dialog

The make-private dialog appears when the owner of a shared wallet wants to convert it into a personal wallet, removing all other members.

**Dialog content:**

- Warning text explaining that all current members (except the owner) will be removed.
- A list of members who will be removed, each showing their display name and role.
- For members who contributed transactions, a note that their transactions will remain in the wallet history.

**Actions:**

- **Confirm** — executes the conversion. All members are removed, invitations are cancelled, and the wallet type changes to Personal.
- **Cancel** — closes the dialog without making changes.

---

## Shared Wallet Member Management

The shared wallet view (`/wallets/:id/members`) shows the member list for a shared wallet and provides invitation and membership management tools.

**Member list:** A table showing each member with:

- Avatar or initials.
- Display name and username.
- Role badge: Owner, Editor, or Guest.
- Joined date.
- **Role selector** (owner only) — a dropdown to change a member's role. The owner's own role cannot be changed from this view.
- **Remove button** (owner only) — removes the member from the wallet. A confirmation dialog appears.
- **Leave button** (for the current user, if not the owner) — allows the user to leave the shared wallet. A confirmation dialog warns that leaving is permanent and the user will lose access to all wallet data.

**Actions at top:**

- **Invite member** (visible to all current members) — navigates to the invite member form.

---

## Invite Member

The invite member view (`/wallets/:id/invite`) displays a centered card with an `InvitationForm`.

**Fields:**

- **Email address** — text input. Required. Must be a valid email address. The invited person does not need to have a Kakeibo account; they will receive an email with a link to join.

**Actions:**

- **Send invitation** — submits the invitation. On success, a confirmation message is shown. The form is reset so the user can immediately invite another person without navigating away.
- **Cancel** — navigates back to the member management view.

**Error handling:**

- HTTP 409: "This person is already a member of this wallet."
- HTTP 422: "This email address is already invited and the invitation is pending."
- HTTP 403: "You don't have permission to invite members to this wallet."
