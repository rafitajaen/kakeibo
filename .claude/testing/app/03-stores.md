# 03 — Testear Pinia Stores

---

## Qué testear en un store

Un store Pinia tiene tres tipos de lógica:

1. **Acciones async**: llaman a la API y mutan el estado → testear happy path + error
2. **Mutaciones de estado**: append, remove, replace de items en listas → testear listas
3. **Computed / getters**: filtros y derivaciones del estado → testear cada caso del filtro

## Qué NO testear

- Que Pinia guarda valores en `ref()`: eso es responsabilidad de Pinia, no tuya.
- Que axios funciona: mockeas axios y testeas tu código, no la librería.
- Detalles de implementación internos (nombres de variables, orden de sentencias).

---

## Setup estándar

Todo test de store tiene la misma estructura de 4 pasos:

```typescript
// 1. Mockear axios ANTES de cualquier import que use el store
//    (Vitest eleva vi.mock() al top del archivo, pero es buena práctica poner el comentario)
vi.mock("@/lib/axios", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    },
}));

// 2. Importar después del mock
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useBudgetsStore } from "@/stores/budgets";
import api from "@/lib/axios";

// 3. Cast tipado del mock para acceder a .mockResolvedValueOnce
const apiMock = api as unknown as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

// 4. Reset en cada test
beforeEach(() => {
    setActivePinia(createPinia()); // Pinia fresca → estado aislado por test
    vi.clearAllMocks();            // Reset de llamadas y valores mockeados
});

afterEach(() => {
    vi.clearAllMocks();
});
```

**Por qué `setActivePinia(createPinia())` en cada test:** Si reutilizamos la misma
instancia, el estado de un test contamina el siguiente. Con una instancia fresca,
cada test empieza con el store en su estado inicial.

---

## Patrón: factory function para datos de test

En lugar de repetir objetos grandes, crea una función que genera un objeto con
valores por defecto y permite sobreescribir sólo lo que necesitas:

```typescript
const makeBudget = (overrides: Record<string, unknown> = {}) => ({
    id: "budget-1",
    name: "Food Budget",
    categoryId: "cat-1",
    categoryName: "Food & Dining",
    limit: 400,
    startDate: "2026-01-01",
    endDate: "2026-01-31",
    walletId: null,
    walletName: null,
    currentSpending: 0,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

// Uso: objeto base
const budget = makeBudget();

// Uso: con override
const exceededBudget = makeBudget({ id: "b-2", currentSpending: 450, limit: 400 });
```

---

## Patrón: testear fetch (happy path + error)

```typescript
describe("fetchBudgets", () => {
    it("populates the list on success", async () => {
        const budget = makeBudget();
        apiMock.get.mockResolvedValueOnce({ data: [budget] });
        const store = useBudgetsStore();

        await store.fetchBudgets();

        expect(store.budgets).toHaveLength(1);
        expect(store.budgets[0].name).toBe("Food Budget");
        expect(store.isLoading).toBe(false); // loading resuelto
    });

    it("passes optional query params when provided", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [] });
        const store = useBudgetsStore();

        await store.fetchBudgets("cat-1", "wallet-1");

        expect(apiMock.get).toHaveBeenCalledWith("/api/budgets", {
            params: { categoryId: "cat-1", walletId: "wallet-1" },
        });
    });

    it("sets isLoading to false even on error", async () => {
        apiMock.get.mockRejectedValueOnce(new Error("Network error"));
        const store = useBudgetsStore();

        await expect(store.fetchBudgets()).rejects.toThrow();

        expect(store.isLoading).toBe(false);
    });
});
```

---

## Patrón: testear mutaciones de estado (CRUD)

```typescript
describe("createBudget", () => {
    it("appends the new budget to the list and returns it", async () => {
        const created = makeBudget({ id: "budget-new", name: "New Budget" });
        apiMock.post.mockResolvedValueOnce({ data: created });
        const store = useBudgetsStore();

        const result = await store.createBudget({
            name: "New Budget",
            categoryId: "cat-1",
            limit: 400,
            startDate: "2026-01-01",
            endDate: "2026-01-31",
        });

        expect(result.id).toBe("budget-new");
        expect(store.budgets).toHaveLength(1);
        expect(store.budgets[0].name).toBe("New Budget");
    });
});

describe("deleteBudget", () => {
    it("removes the entry from the list", async () => {
        const budget = makeBudget();
        // Pre-cargar estado: fetch primero, luego delete
        apiMock.get.mockResolvedValueOnce({ data: [budget] });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useBudgetsStore();
        await store.fetchBudgets();

        await store.deleteBudget("budget-1");

        expect(store.budgets).toHaveLength(0);
        expect(apiMock.delete).toHaveBeenCalledWith("/api/budgets/budget-1");
    });
});

describe("updateBudget", () => {
    it("replaces the entry in the list in-place", async () => {
        const original = makeBudget();
        const updated = makeBudget({ name: "Updated Name", limit: 600 });
        apiMock.get.mockResolvedValueOnce({ data: [original] });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        await store.updateBudget("budget-1", { name: "Updated Name", limit: 600 });

        expect(store.budgets[0].name).toBe("Updated Name");
        expect(store.budgets[0].limit).toBe(600);
        expect(store.budgets).toHaveLength(1); // no se duplicó
    });
});
```

---

## Patrón: testear computed / getters

Los computed se testean igual que acciones: cargas estado en el store y verificas
el valor derivado.

```typescript
describe("computed: onTrackBudgets / warningBudgets / exceededBudgets", () => {
    it("onTrackBudgets returns budgets below 75% threshold", async () => {
        const low = makeBudget({ id: "b-1", currentSpending: 74, limit: 100 });
        const warn = makeBudget({ id: "b-2", currentSpending: 75, limit: 100 });
        apiMock.get.mockResolvedValueOnce({ data: [low, warn] });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        // 74/100 = 74% → on track; 75/100 = 75% → warning boundary
        expect(store.onTrackBudgets).toHaveLength(1);
        expect(store.onTrackBudgets[0].id).toBe("b-1");
    });

    it("exceededBudgets returns budgets at or above 100%", async () => {
        const ok = makeBudget({ id: "b-1", currentSpending: 399, limit: 400 });
        const exceeded = makeBudget({ id: "b-2", currentSpending: 400, limit: 400 });
        apiMock.get.mockResolvedValueOnce({ data: [ok, exceeded] });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        expect(store.exceededBudgets).toHaveLength(1);
        expect(store.exceededBudgets[0].id).toBe("b-2");
    });
});
```

---

## Inventario de stores con su spec

| Store | Spec | Descripción |
|-------|------|-------------|
| `stores/auth.ts` | *(sin spec individual)* | Login, register, sesiones, token refresh |
| `stores/wallets.ts` | `test/stores/wallets.spec.ts` | CRUD, invitaciones, deudas |
| `stores/transactions.ts` | `test/stores/transactions.spec.ts` | Registro, edición, paginación |
| `stores/categories.ts` | `test/stores/categories.spec.ts` | Fetch, crear, archivar |
| `stores/budgets.ts` | `test/stores/budgets.spec.ts` | CRUD, monitoring, computed |
| `stores/goals.ts` | `test/stores/goals.spec.ts` | CRUD, progreso, computed |
| `stores/recurring.ts` | *(sin spec individual)* | Patrones, forecast |
| `stores/notifications.ts` | *(sin spec individual)* | Fetch, marcar leído, preferencias |
| `stores/activity.ts` | *(sin spec individual)* | Feed, filtros |
| `stores/friends.ts` | *(sin spec individual)* | Solicitudes, perfiles |
| `stores/settings.ts` | *(sin spec individual)* | Preferencias del usuario |
| `stores/admin.ts` | *(sin spec individual)* | Modo mantenimiento, gestión usuarios |
| `stores/importExport.ts` | *(sin spec individual)* | CSV import/export |

Specs marcados como "sin spec individual" están parcialmente cubiertos por tests E2E.
Añadir specs unitarios cuando se toque ese store en una nueva fase.
