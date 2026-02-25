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
    {
        path: "/",
        name: "home",
        component: () => import("@/views/HomeView.vue"),
        meta: { requiresAuth: true },
    },
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
    {
        path: "/wallets",
        name: "wallets",
        component: () => import("@/views/wallets/WalletsView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/new",
        name: "wallets-create",
        component: () => import("@/views/wallets/CreateWalletView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:id",
        name: "wallet-detail",
        component: () => import("@/views/wallets/WalletDetailView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:id/edit",
        name: "wallet-edit",
        component: () => import("@/views/wallets/EditWalletView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:id/members",
        name: "wallet-members",
        component: () => import("@/views/wallets/SharedWalletView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:id/invite",
        name: "wallet-invite",
        component: () => import("@/views/wallets/InviteMemberView.vue"),
        meta: { requiresAuth: true },
    },
    {
        // No requiresAuth — view handles unauthenticated users with redirect-to-login flow.
        path: "/wallets/invitations/:code/accept",
        name: "accept-invitation",
        component: () => import("@/views/wallets/AcceptInvitationView.vue"),
    },
    {
        path: "/categories",
        name: "categories",
        component: () => import("@/views/categories/CategoriesView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/categories/new",
        name: "category-create",
        component: () => import("@/views/categories/CreateCategoryView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/categories/:id/edit",
        name: "category-edit",
        component: () => import("@/views/categories/EditCategoryView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:walletId/transactions",
        name: "transactions",
        component: () => import("@/views/transactions/TransactionsView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:walletId/transactions/new",
        name: "transaction-record",
        component: () => import("@/views/transactions/RecordTransactionView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets/:walletId/transactions/:id/edit",
        name: "transaction-edit",
        component: () => import("@/views/transactions/EditTransactionView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/budgets",
        name: "budgets",
        component: () => import("@/views/budgets/BudgetsView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/budgets/new",
        name: "budget-create",
        component: () => import("@/views/budgets/CreateBudgetView.vue"),
        meta: { requiresAuth: true },
    },
    {
        path: "/budgets/:id/edit",
        name: "budget-edit",
        component: () => import("@/views/budgets/EditBudgetView.vue"),
        meta: { requiresAuth: true },
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
