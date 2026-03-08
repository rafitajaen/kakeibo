# 08 — Testear Guards y Navegación

---

## Cómo funcionan los guards en este proyecto

El router de Kakeibo tiene dos guards principales en `beforeEach`:

- **`requiresAuth`**: Si la ruta tiene `meta.requiresAuth = true` y el usuario no está
  autenticado (store de auth sin usuario), redirige a `/login`.
- **`requiresGuest`**: Si la ruta tiene `meta.requiresGuest = true` y el usuario ya está
  autenticado, redirige a `/` (o a `/onboarding` si no tiene wallets).

Testear estos guards en unit tests es útil para verificar la lógica de redirección
sin levantar un navegador completo.

---

## Setup: router + store mock

```typescript
import { describe, it, expect, vi, beforeEach } from "vitest";
import { createRouter, createMemoryHistory } from "vue-router";
import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "@/stores/auth";
import { useWalletsStore } from "@/stores/wallets";

// Mockear axios para evitar llamadas HTTP reales en los guards
vi.mock("@/lib/axios", () => ({
    default: { get: vi.fn() },
}));

// Rutas mínimas que replican la configuración real del router
const routes = [
    {
        path: "/login",
        name: "login",
        component: { template: "<div />" },
        meta: { requiresGuest: true },
    },
    {
        path: "/",
        name: "home",
        component: { template: "<div />" },
        meta: { requiresAuth: true },
    },
    {
        path: "/wallets",
        name: "wallets",
        component: { template: "<div />" },
        meta: { requiresAuth: true },
    },
    {
        path: "/onboarding",
        name: "onboarding",
        component: { template: "<div />" },
        meta: { requiresAuth: true },
    },
];

// Importar el guard real del proyecto
import { setupRouterGuards } from "@/router/guards"; // ajusta el path si es diferente

function createTestRouter() {
    const router = createRouter({
        history: createMemoryHistory(),
        routes,
    });
    setupRouterGuards(router); // registrar los guards reales
    return router;
}

beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
});
```

---

## Testear `requiresAuth`: redirección cuando no hay sesión

```typescript
describe("requiresAuth guard", () => {
    it("redirects unauthenticated user to /login", async () => {
        const router = createTestRouter();
        const authStore = useAuthStore();
        authStore.user = null; // sin usuario

        await router.push("/wallets");
        await router.isReady();

        // El guard debe haber redirigido a /login
        expect(router.currentRoute.value.name).toBe("login");
    });

    it("allows authenticated user to access protected route", async () => {
        const router = createTestRouter();
        const authStore = useAuthStore();
        // Simular usuario autenticado
        authStore.user = {
            id: "user-1",
            email: "test@example.com",
            role: "User",
            currency: "EUR",
            isVerified: true,
            name: "Test User",
        };
        const walletsStore = useWalletsStore();
        walletsStore.wallets = [/* al menos una wallet */];

        await router.push("/wallets");
        await router.isReady();

        expect(router.currentRoute.value.name).toBe("wallets");
    });
});
```

---

## Testear `requiresGuest`: redirección cuando ya hay sesión

```typescript
describe("requiresGuest guard", () => {
    it("redirects authenticated user away from /login to /", async () => {
        const router = createTestRouter();
        const authStore = useAuthStore();
        authStore.user = {
            id: "user-1",
            email: "test@example.com",
            role: "User",
            currency: "EUR",
            isVerified: true,
            name: "Test User",
        };
        const walletsStore = useWalletsStore();
        walletsStore.wallets = [{ id: "w-1", name: "Checking", type: "Personal",
            currency: "EUR", balance: 0, isArchived: false, createdAt: "" }];

        await router.push("/login");
        await router.isReady();

        // Usuario con wallets → redirige a home, no a login
        expect(router.currentRoute.value.name).toBe("home");
    });

    it("redirects authenticated user with no wallets to /onboarding", async () => {
        const router = createTestRouter();
        const authStore = useAuthStore();
        authStore.user = {
            id: "user-1",
            email: "test@example.com",
            role: "User",
            currency: "EUR",
            isVerified: true,
            name: null,
        };
        const walletsStore = useWalletsStore();
        walletsStore.wallets = []; // sin wallets → onboarding

        await router.push("/login");
        await router.isReady();

        expect(router.currentRoute.value.name).toBe("onboarding");
    });

    it("allows unauthenticated user to access /login", async () => {
        const router = createTestRouter();
        const authStore = useAuthStore();
        authStore.user = null;

        await router.push("/login");
        await router.isReady();

        expect(router.currentRoute.value.name).toBe("login");
    });
});
```

---

## Testear rutas con parámetros (`:id`, `:code`)

```typescript
describe("routes with parameters", () => {
    it("wallet-detail route resolves with walletId param", async () => {
        const routes = [
            ...sharedRoutes,
            {
                path: "/wallets/:walletId",
                name: "wallet-detail",
                component: { template: "<div />" },
                meta: { requiresAuth: true },
            },
        ];
        const router = createRouter({ history: createMemoryHistory(), routes });
        setupRouterGuards(router);

        // Simular usuario autenticado
        const authStore = useAuthStore();
        authStore.user = { id: "u-1", email: "", role: "User",
            currency: "EUR", isVerified: true, name: null };

        await router.push("/wallets/wallet-abc-123");
        await router.isReady();

        expect(router.currentRoute.value.name).toBe("wallet-detail");
        expect(router.currentRoute.value.params.walletId).toBe("wallet-abc-123");
    });

    it("invitation route resolves with code param", async () => {
        const routes = [
            ...sharedRoutes,
            {
                path: "/invitations/:code",
                name: "invitation-accept",
                component: { template: "<div />" },
                // Las invitaciones no requieren auth (usuario puede no estar registrado)
            },
        ];
        const router = createRouter({ history: createMemoryHistory(), routes });
        setupRouterGuards(router);

        await router.push("/invitations/ABC-123-XYZ");
        await router.isReady();

        expect(router.currentRoute.value.name).toBe("invitation-accept");
        expect(router.currentRoute.value.params.code).toBe("ABC-123-XYZ");
    });
});
```

---

## Cuándo usar router tests vs E2E para guards

| Escenario | Preferir |
|-----------|---------|
| Verificar lógica de redirección del guard | Router test (unitario, rápido) |
| Verificar que la página de login se renderiza correctamente | E2E |
| Verificar flujo completo: acceder → redirigir → iniciar sesión → volver | E2E |
| Verificar que `meta.requiresAuth` está en la ruta correcta | Router test |

Los router tests son muy rápidos y no requieren un navegador. Son perfectos para
verificar la lógica de redirección de forma exhaustiva. Los E2E validan que el
flujo completo desde la perspectiva del usuario funciona correctamente.
