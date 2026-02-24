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
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
});

// Global navigation guard — enforces auth/guest route protection.
router.beforeEach(async (to) => {
    const auth = useAuthStore();

    // Resolve user from session cookies if not yet loaded.
    if (auth.user === null) {
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
