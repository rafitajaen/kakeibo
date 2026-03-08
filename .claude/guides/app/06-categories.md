# 06 — Categories

## Purpose

The Categories section lets users view the 12 built-in system categories and manage their own custom categories. Categories are used to classify every transaction.

---

## Categories List View

The categories list view (`/categories`) uses a two-tab interface:

**System tab** — shows the 12 non-deletable system categories provided by the platform. These categories cannot be edited, archived, or deleted. Each entry in the list shows the category icon, name, and an indication that it is a system category. No action buttons are shown.

The 12 system categories are:
1. Housing
2. Transportation
3. Food & Dining
4. Health & Wellness
5. Entertainment & Leisure
6. Shopping & Personal
7. Education
8. Subscriptions & Bills
9. Savings & Investments
10. Debt & Loans
11. Gifts & Donations
12. Other

**Custom tab** — shows the user's custom categories. Each entry shows the category icon, name, colors (via a visual pill preview), and a privacy indicator if the category is private. Actions available on each item:

- **Edit** — navigates to the edit category view.
- **Archive** — archives the category. Archived categories are excluded from transaction and budget forms but their historical transactions retain the category reference. A confirmation dialog appears before archiving.
- **Unarchive** — available only on archived categories (shown in a separate archived sub-section or toggled via a filter). Restores the category to active status.

A **New category** button at the top right navigates to the category creation form.

If the user has no custom categories, an empty state message is shown with an invitation to create the first one.

---

## Create Category

The create category view (`/categories/new`) displays a centered card with a `CategoryForm` in creation mode.

---

## Edit Category

The edit category view (`/categories/:id/edit`) uses the same `CategoryForm` in edit mode. System categories can technically be visited at this URL but the Name field is disabled and no save action is permitted — they are effectively read-only.

For the user's own custom categories, all fields are editable.

---

## CategoryForm

The category form is the shared form component for creating and editing custom categories.

**Fields:**

- **Name** — a text input. Required. Maximum 50 characters. Must be unique among the user's categories (system category names are reserved). In edit mode for system categories, this field is disabled.

- **Icon (collapsible section)** — an icon picker showing a large grid of named lucide icons. The section is collapsed by default. Clicking the section header expands it. A search input within the picker filters icons by name. Selecting an icon assigns it to the category and collapses the section, showing the selected icon's name next to the toggle. Optional — a category can exist without a custom icon, in which case a default placeholder is used.

- **Background color** — a color picker using the HTML5 native `<input type="color">`. Optional. Default is `#3B82F6`. Controls the pill background color used when displaying this category in lists and the transaction form.

- **Text color** — a color picker. Optional. Default is `#FFFFFF`. Controls the text and icon color on the pill.

- **Preview** — a live-updating visual card showing exactly how the category pill will appear in the app with the currently selected name, icon, background color, and text color. The preview updates in real time as the user makes changes.

- **Is Private** — a checkbox. When checked, the category is marked as private. Private categories are visible only to the owner and are not shown to other members when the owner records transactions in a shared wallet. The field shows a hint: "This category will only be visible to you in shared wallets." Optional, unchecked by default.

**Actions:**

- **Save** (or "Create" in create mode) — submits the form. On success, navigates to the categories list.
- **Cancel** — navigates back to the categories list without saving.

**Error handling:**

- HTTP 409: "A category with this name already exists."
- HTTP 403: "You don't have permission to edit this category" (shown when a user attempts to edit a system category via direct URL).
- HTTP 404: "Category not found."
- Inline validation errors for required fields.
