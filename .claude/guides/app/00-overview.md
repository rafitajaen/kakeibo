# 00 — App Overview: Layout, Navigation, and Shell

## Purpose

This document describes the overall shell of the Kakeibo web app: the layout wrapper, sidebar navigation, breadcrumb system, and mobile interaction patterns. Every authenticated screen lives inside this shell.

---

## Public vs Authenticated Routes

The router separates two access tiers. Public routes (login, register, email verification, forgot password, reset password, invitation acceptance, and onboarding) render without the app shell — they are standalone centered-card pages. Authenticated routes are all nested under `AppLayout`, which provides the sidebar, header, and notification bell.

When an unauthenticated user attempts to reach an authenticated route, the router guard redirects them to `/login`. After successful login, the app redirects back to the originally requested URL. When an authenticated user visits a public route, they are redirected to the dashboard.

---

## AppLayout

`AppLayout` is the root wrapper for all authenticated views. It renders a `SidebarProvider` that manages the collapsed/expanded state of the sidebar and passes it down via context. Inside, it composes three structural elements: the `AppSidebar` on the left, a `SidebarInset` content area on the right, and a `SiteHeader` fixed at the top of the content area.

The sidebar and header are always present regardless of which authenticated route is active. The main content area renders the matched route component via `<RouterView>`.

---

## AppSidebar

The sidebar is collapsible using `collapsible="icon"` mode. When expanded, it shows icon plus label for every navigation item. When collapsed, it shows only icons with tooltips. The user can toggle it via the trigger button in the header.

The sidebar is composed of three sections stacked vertically:

**Logo area (top):** Displays the Kakeibo logotype or icon depending on collapsed state. Clicking it navigates to the dashboard.

**NavMain (middle, scrollable):** The primary navigation group. Items correspond to the main sections of the app. Each item shows a lucide icon and a label. Active route is highlighted. The items are:

- Dashboard (home icon) → `/`
- Wallets → `/wallets`
- Transactions → `/transactions`
- Categories → `/categories`
- Budgets → `/budgets`
- Goals → `/goals`
- Recurring → `/recurring`
- Friends → `/friends`
- Activity → `/activity`
- Notifications → `/notifications`
- Settings → `/settings`
- Admin → `/admin` (visible only to users with the Admin role)

**NavUser (footer):** Shows the logged-in user's avatar, display name, and email in a dropdown trigger. Clicking opens a dropdown menu with links to Settings and a Sign out action.

---

## SiteHeader

The header sits at the top of the `SidebarInset` and contains three elements aligned horizontally:

**SidebarTrigger:** A button on the far left that toggles the sidebar between expanded and collapsed states.

**Separator:** A visual divider between the trigger and the breadcrumb.

**Breadcrumb:** A dynamic breadcrumb showing the current location within the app. The breadcrumb is derived from the current route name using a `routeTitleMap` lookup. Nested routes show intermediate segments. The breadcrumb is rendered with shadcn-vue's Breadcrumb component.

**Notification Bell (right side):** A bell icon button showing a badge with the count of unread in-app notifications. Clicking it opens a slide-over or popover panel listing recent unread notifications. Each notification shows its message and timestamp. A "Mark all as read" action clears the badge. The bell fetches the unread count on mount and on each route change.

---

## Mobile and Responsive Behavior

The layout adapts to smaller screens. On mobile, the sidebar defaults to collapsed and the trigger opens it as an overlay drawer. The notification bell remains visible. The content area uses responsive padding so that cards and forms remain readable at narrow widths.

Navigation items in the sidebar remain the same on all screen sizes. There is no separate bottom tab bar — the sidebar drawer serves as the primary navigation on mobile.

---

## Toast Notifications

The app uses a global toast/snackbar system for transient feedback (success and error messages after form submissions, background actions, etc.). Toasts appear at the bottom-right of the screen and auto-dismiss after a few seconds. They use shadcn-vue's Sonner integration. Individual views trigger toasts via a `useToast` composable after API calls complete.

---

## Loading and Error States

Route-level loading is not indicated with a top bar progress indicator; instead, each view manages its own loading state with skeleton loaders or spinner icons rendered inside the content area. If a required resource fails to load (e.g., a wallet not found), the view renders an inline error message with a retry or back navigation option.
