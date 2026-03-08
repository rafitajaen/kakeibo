import { createRouter, createWebHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import { useAuthStore } from "@/stores/auth";

declare module "vue-router" {
    interface RouteMeta {
        // Route requires authenticated session.
        requiresAuth?: boolean;
        // Route is only accessible when NOT authenticated (e.g. login, register).
        requiresGuest?: boolean;
    }
}

const routes: RouteRecordRaw[] = [
    // Public routes (no AppLayout)
    {
        path: "/login",
        name: "login",
        component: () => import("@/views/auth/LoginView.vue"),
        meta: { requiresGuest: true },
    },
    {
        path: "/register",
        name: "register",
        component: () => import("@/views/auth/RegisterView.vue"),
        meta: { requiresGuest: true },
    },
    {
        path: "/verify-email",
        name: "verify-email",
        component: () => import("@/views/auth/VerifyEmailView.vue"),
    },
    {
        path: "/forgot-password",
        name: "forgot-password",
        component: () => import("@/views/auth/ForgotPasswordView.vue"),
        meta: { requiresGuest: true },
    },
    {
        path: "/reset-password",
        name: "reset-password",
        component: () => import("@/views/auth/ResetPasswordView.vue"),
        meta: { requiresGuest: true },
    },
    // Onboarding — authenticated but outside AppLayout (full-page wizard).
    {
        path: "/onboarding",
        name: "onboarding",
        component: () => import("@/views/onboarding/OnboardingView.vue"),
        meta: { requiresAuth: true },
    },
    {
        // No requiresAuth — view handles unauthenticated users with redirect-to-login flow.
        path: "/wallets/invitations/:code/accept",
        name: "accept-invitation",
        component: () => import("@/views/wallets/AcceptInvitationView.vue"),
    },

    // Authenticated routes — nested under AppLayout (header + sidebar + NotificationBell)
    {
        path: "/",
        component: () => import("@/components/AppLayout.vue"),
        meta: { requiresAuth: true },
        children: [
            {
                path: "",
                name: "home",
                component: () => import("@/views/HomeView.vue"),
            },
            {
                path: "wallets",
                name: "wallets",
                component: () => import("@/views/wallets/WalletsView.vue"),
            },
            {
                path: "wallets/new",
                name: "wallets-create",
                component: () => import("@/views/wallets/CreateWalletView.vue"),
            },
            {
                path: "wallets/:id",
                name: "wallet-detail",
                component: () => import("@/views/wallets/WalletDetailView.vue"),
            },
            {
                path: "wallets/:id/edit",
                name: "wallet-edit",
                component: () => import("@/views/wallets/EditWalletView.vue"),
            },
            {
                path: "wallets/:id/members",
                name: "wallet-members",
                component: () => import("@/views/wallets/SharedWalletView.vue"),
            },
            {
                path: "wallets/:id/invite",
                name: "wallet-invite",
                component: () => import("@/views/wallets/InviteMemberView.vue"),
            },
            {
                path: "categories",
                name: "categories",
                component: () => import("@/views/categories/CategoriesView.vue"),
            },
            {
                path: "categories/new",
                name: "category-create",
                component: () => import("@/views/categories/CreateCategoryView.vue"),
            },
            {
                path: "categories/:id/edit",
                name: "category-edit",
                component: () => import("@/views/categories/EditCategoryView.vue"),
            },
            {
                path: "wallets/:walletId/transactions",
                name: "transactions",
                component: () => import("@/views/transactions/TransactionsView.vue"),
            },
            {
                path: "wallets/:walletId/transactions/new",
                name: "transaction-record",
                component: () => import("@/views/transactions/RecordTransactionView.vue"),
            },
            {
                path: "wallets/:walletId/transactions/:id/edit",
                name: "transaction-edit",
                component: () => import("@/views/transactions/EditTransactionView.vue"),
            },
            {
                path: "budgets",
                name: "budgets",
                component: () => import("@/views/budgets/BudgetsView.vue"),
            },
            {
                path: "budgets/new",
                name: "budget-create",
                component: () => import("@/views/budgets/CreateBudgetView.vue"),
            },
            {
                path: "budgets/:id/edit",
                name: "budget-edit",
                component: () => import("@/views/budgets/EditBudgetView.vue"),
            },
            {
                path: "goals",
                name: "goals",
                component: () => import("@/views/goals/GoalsView.vue"),
            },
            {
                path: "goals/new",
                name: "goal-create",
                component: () => import("@/views/goals/CreateGoalView.vue"),
            },
            {
                path: "goals/:id/edit",
                name: "goal-edit",
                component: () => import("@/views/goals/EditGoalView.vue"),
            },
            {
                path: "recurring",
                name: "recurring",
                component: () => import("@/views/recurring/RecurringView.vue"),
            },
            {
                path: "recurring/new",
                name: "recurring-create",
                component: () => import("@/views/recurring/CreatePatternView.vue"),
            },
            {
                path: "recurring/:id/edit",
                name: "recurring-edit",
                component: () => import("@/views/recurring/EditPatternView.vue"),
            },
            {
                path: "notifications/preferences",
                name: "notification-preferences",
                component: () => import("@/views/notifications/PreferencesView.vue"),
            },
            {
                path: "activity",
                name: "activity",
                component: () => import("@/views/activity/ActivityView.vue"),
            },
            {
                path: "friends",
                name: "friends",
                component: () => import("@/views/friends/FriendsView.vue"),
            },
            {
                path: "friends/requests",
                name: "friend-requests",
                component: () => import("@/views/friends/FriendRequestsView.vue"),
            },
            {
                path: "users/:id",
                name: "user-profile",
                component: () => import("@/views/friends/UserProfileView.vue"),
            },
            {
                path: "settings",
                name: "settings",
                component: () => import("@/views/settings/SettingsView.vue"),
            },
            {
                path: "admin",
                name: "admin",
                component: () => import("@/views/admin/AdminView.vue"),
            },
        ],
    },
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

// Global navigation guard — enforces auth/guest route protection.
router.beforeEach(async (to) => {
    const auth = useAuthStore();

    // Resolve user from session cookies on first navigation only.
    if (!auth.initialized) {
        await auth.fetchCurrentUser();
    }

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        return { name: "login" };
    }

    if (to.meta.requiresGuest && auth.isAuthenticated) {
        return { name: "home" };
    }
});

export default router;
