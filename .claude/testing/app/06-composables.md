# 06 — Testear Composables

---

## Qué es un composable y por qué es especial

Un composable es una función que usa la Composition API de Vue (`ref`, `computed`, `watch`, etc.)
para encapsular lógica reactiva reutilizable. El problema al testearlos en aislamiento es que
`ref()`, `computed()` y `onMounted()` solo funcionan dentro de un contexto de componente activo.

Solución: **wrappear la llamada al composable dentro de un componente mínimo** usando
Vue Test Utils.

---

## Helper `withSetup()`

Este helper crea un componente temporal, llama al composable dentro de él y devuelve
el resultado. Úsalo en todos los tests de composables que usen `onMounted` o `watch`.

```typescript
import { defineComponent, h } from "vue";
import { mount } from "@vue/test-utils";

/**
 * Ejecuta un composable dentro de un componente mínimo para
 * activar el contexto de Vue (ref, computed, onMounted, watch).
 */
function withSetup<T>(composable: () => T): [T, ReturnType<typeof mount>] {
    let result!: T;
    const TestComponent = defineComponent({
        setup() {
            result = composable();
            return () => h("div");
        },
    });
    const wrapper = mount(TestComponent, {
        global: { plugins: [createPinia()] },
    });
    return [result, wrapper];
}
```

Si el composable es una función pura sin `onMounted`/`watch` (como `useCurrencyFormat`),
puedes llamarlo directamente sin `withSetup()`.

---

## Testear `useCurrencyFormat`

`useCurrencyFormat` es un composable simple que devuelve una función de formateo.
No usa `onMounted` ni estado reactivo, así que se puede testear directamente.

```typescript
import { describe, it, expect } from "vitest";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

describe("useCurrencyFormat", () => {
    it("formats positive amount as currency string", () => {
        const { formatCurrency } = useCurrencyFormat("EUR");
        const result = formatCurrency(1234.56);
        // El formato exacto depende de la locale del entorno, pero el número debe aparecer
        expect(result).toContain("1.234");  // separador de miles en español
        expect(result).toMatch(/EUR|€/);
    });

    it("formats zero correctly", () => {
        const { formatCurrency } = useCurrencyFormat("USD");
        const result = formatCurrency(0);
        expect(result).toContain("0");
    });

    it("formats negative amount correctly", () => {
        const { formatCurrency } = useCurrencyFormat("USD");
        const result = formatCurrency(-50.25);
        expect(result).toContain("50.25");
        expect(result).toMatch(/-|\(.*\)/); // formato negativo: -$50.25 o ($50.25)
    });
});
```

---

## Testear `useDashboardData`

`useDashboardData` usa `onMounted` y depende del store de transacciones.
Necesitamos `withSetup()` y mockear el store o axios.

```typescript
import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import en from "@/locales/en.json";
import { useDashboardData } from "@/composables/useDashboardData";
import { withSetup } from "../helpers/withSetup"; // helper del proyecto

vi.mock("@/lib/axios", () => ({
    default: { get: vi.fn() },
}));

import api from "@/lib/axios";
const apiMock = api as unknown as { get: ReturnType<typeof vi.fn> };

const mockTransactions = [
    {
        id: "tx-1",
        type: "Income",
        amount: 2000,
        date: "2026-03-01",
        categoryId: "cat-1",
        walletId: "w-1",
        description: "Salary",
    },
    {
        id: "tx-2",
        type: "Expense",
        amount: 150,
        date: "2026-03-02",
        categoryId: "cat-2",
        walletId: "w-1",
        description: "Groceries",
    },
];

describe("useDashboardData", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    it("fetches transactions for the last 90 days on setup", async () => {
        apiMock.get.mockResolvedValueOnce({ data: mockTransactions });
        const [data] = withSetup(() => useDashboardData());
        await Promise.resolve(); // esperar tick del onMounted

        expect(apiMock.get).toHaveBeenCalledWith(
            expect.stringContaining("/api/transactions"),
            expect.objectContaining({ params: expect.objectContaining({ days: 90 }) })
        );
        expect(data.isLoading.value).toBe(false);
    });

    it("computes totalIncome from income transactions", async () => {
        apiMock.get.mockResolvedValueOnce({ data: mockTransactions });
        const [data] = withSetup(() => useDashboardData());
        await Promise.resolve();

        expect(data.sectionMetrics.value.totalIncome).toBe(2000);
    });

    it("computes totalExpenses from expense transactions", async () => {
        apiMock.get.mockResolvedValueOnce({ data: mockTransactions });
        const [data] = withSetup(() => useDashboardData());
        await Promise.resolve();

        expect(data.sectionMetrics.value.totalExpenses).toBe(150);
    });

    it("computes netSavings as income minus expenses", async () => {
        apiMock.get.mockResolvedValueOnce({ data: mockTransactions });
        const [data] = withSetup(() => useDashboardData());
        await Promise.resolve();

        expect(data.sectionMetrics.value.netSavings).toBe(1850);
    });
});
```

---

## Testear `usePushNotifications`

`usePushNotifications` depende de `navigator.serviceWorker` y `PushManager`.
Estos no existen en jsdom, así que los mockeamos globalmente.

```typescript
import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { usePushNotifications } from "@/composables/usePushNotifications";
import { withSetup } from "../helpers/withSetup";

// Mock del ServiceWorker API que no existe en jsdom
const mockSubscription = {
    endpoint: "https://push.example.com/sub-1",
    toJSON: () => ({ endpoint: "https://push.example.com/sub-1" }),
};

const mockRegistration = {
    pushManager: {
        getSubscription: vi.fn().mockResolvedValue(null),
        subscribe: vi.fn().mockResolvedValue(mockSubscription),
    },
};

vi.mock("@/lib/axios", () => ({
    default: { post: vi.fn(), delete: vi.fn() },
}));

import api from "@/lib/axios";
const apiMock = api as unknown as {
    post: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

describe("usePushNotifications", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();

        // Simular que serviceWorker está disponible
        Object.defineProperty(navigator, "serviceWorker", {
            value: {
                ready: Promise.resolve(mockRegistration),
                register: vi.fn().mockResolvedValue(mockRegistration),
            },
            writable: true,
        });
    });

    it("isSupported is false when serviceWorker is not available", () => {
        Object.defineProperty(navigator, "serviceWorker", {
            value: undefined,
            writable: true,
        });
        const [result] = withSetup(() => usePushNotifications());
        expect(result.isSupported.value).toBe(false);
    });

    it("isSupported is true when serviceWorker is available", () => {
        const [result] = withSetup(() => usePushNotifications());
        expect(result.isSupported.value).toBe(true);
    });

    it("subscribe calls the API to save the subscription", async () => {
        apiMock.post.mockResolvedValueOnce({ data: { success: true } });
        const [result] = withSetup(() => usePushNotifications());

        await result.subscribe();

        expect(apiMock.post).toHaveBeenCalledWith(
            expect.stringContaining("/api/notifications/subscriptions"),
            expect.objectContaining({ endpoint: mockSubscription.endpoint })
        );
    });
});
```

---

## Inventario de composables del proyecto

| Composable | Ubicación | Propósito | Necesita `withSetup` |
|-----------|-----------|-----------|---------------------|
| `useCurrencyFormat` | `composables/useCurrencyFormat.ts` | Formateo de moneda con `Intl.NumberFormat` | No |
| `useDashboardData` | `composables/useDashboardData.ts` | Fetch 90d, métricas, datos de chart | Sí (`onMounted`) |
| `usePushNotifications` | `composables/usePushNotifications.ts` | VAPID WebPush, suscripción | Sí (`onMounted`) |
