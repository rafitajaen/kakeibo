# 05 — Testear Vistas (Views / Páginas)

---

## Diferencia entre view y component

Un **componente** (`components/`) es una pieza reutilizable: un card, un form, un badge.
Una **vista** (`views/`) es una página completa: `WalletsView.vue`, `BudgetsView.vue`.

Las vistas orquestan componentes. Se encargan de:
- Llamar al store para obtener datos al montar
- Pasar datos a los componentes hijos como props
- Escuchar eventos de los hijos y llamar a acciones del store
- Controlar qué se muestra según el estado (loading, vacío, error)

---

## ¿View test o E2E?

Esta es la pregunta más importante al enfrentarse a una vista nueva:

```
¿Necesito testear la vista?
│
├── ¿El flujo involucra navegación entre páginas?
│   └── → E2E (ver 09-e2e.md)
│
├── ¿El flujo requiere autenticación real?
│   └── → E2E (ver 09-e2e.md)
│
├── ¿Solo quiero verificar que los subcomponentes se renderizan?
│   └── → Ya están cubiertos por tests de componente (ver 04-components.md)
│
└── ¿Quiero testear lógica de orquestación aislada (renderizado condicional,
    llamada al store en onMounted, manejo de errores)?
    └── → View test unitario (este documento)
```

**Regla práctica:** Las vistas de Kakeibo están principalmente cubiertas por E2E.
Un test de view unitario es útil cuando hay lógica condicional compleja que
sería difícil de reproducir en E2E (estados de carga, estados vacíos, manejo de errores).

---

## Setup: router mock, store mocks, i18n

```typescript
import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import { createRouter, createMemoryHistory } from "vue-router";
import { createI18n } from "vue-i18n";
import BudgetsView from "@/views/budgets/BudgetsView.vue";
import { useBudgetsStore } from "@/stores/budgets";
import en from "@/locales/en.json";

vi.mock("@/lib/axios", () => ({
    default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import api from "@/lib/axios";
const apiMock = api as unknown as { get: ReturnType<typeof vi.fn> };

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

// Router mínimo: solo las rutas que la vista necesita
const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        { path: "/", name: "home", component: { template: "<div />" } },
        { path: "/budgets", name: "budgets", component: BudgetsView },
        { path: "/budgets/new", name: "budgets-create", component: { template: "<div />" } },
    ],
});

beforeEach(async () => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    // Navegar a la ruta de la vista antes de cada test
    await router.push("/budgets");
    await router.isReady();
});

function mountView() {
    return mount(BudgetsView, {
        global: {
            plugins: [i18n, createPinia(), router],
            stubs: {
                // Stub de componentes hijos complejos para aislar la vista
                BudgetList: { template: '<div data-testid="budget-list" />' },
                BudgetForm: { template: '<div data-testid="budget-form" />' },
            },
        },
    });
}
```

---

## Qué testear en una view

### 1. Estado de carga (loading)

```typescript
it("shows a loading spinner while fetching budgets", async () => {
    // Mockear una promise que nunca resuelve (estado de carga infinita)
    apiMock.get.mockReturnValue(new Promise(() => {}));
    const wrapper = mountView();

    expect(wrapper.find('[data-testid="loading"]').exists()).toBe(true);
});
```

### 2. Estado vacío

```typescript
it("shows empty state message when there are no budgets", async () => {
    apiMock.get.mockResolvedValueOnce({ data: [] });
    const wrapper = mountView();

    // Esperar a que se resuelva el fetch
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick(); // segundo tick por si hay watchers

    expect(wrapper.text()).toContain("No budgets yet");
});
```

### 3. Llamada al store en onMounted

```typescript
it("calls fetchBudgets on mount", async () => {
    apiMock.get.mockResolvedValueOnce({ data: [] });
    const wrapper = mountView();
    await wrapper.vm.$nextTick();

    expect(apiMock.get).toHaveBeenCalledWith(
        expect.stringContaining("/api/budgets"),
        expect.anything()
    );
});
```

### 4. Renderizado condicional según estado del store

```typescript
it("renders BudgetList when budgets are loaded", async () => {
    apiMock.get.mockResolvedValueOnce({ data: [makeBudget()] });
    const wrapper = mountView();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    expect(wrapper.find('[data-testid="budget-list"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false);
});
```

---

## Cuándo un E2E es mejor que un view test

Prefiere E2E cuando:
- La vista hace navegación (`router.push(...)`) tras una acción del usuario
- La vista depende del guard de autenticación para mostrar su contenido
- Necesitas verificar el comportamiento de un formulario completo (submit, validación, redirección)
- El test requiere múltiples stores interactuando entre sí

Los tests de vista unitarios son útiles para verificar estados específicos
(loading, vacío, error) que son difíciles de provocar de forma confiable en E2E.

---

## Vistas actuales y cobertura

En la fase actual, las vistas no tienen spec unitario propio: están cubiertas
por los tests E2E de cada dominio.

| Vista | Cobertura actual |
|-------|----------------|
| `views/auth/LoginView.vue` | `e2e/auth.spec.ts` |
| `views/auth/RegisterView.vue` | `e2e/auth.spec.ts` |
| `views/dashboard/DashboardView.vue` | `e2e/home.spec.ts`, `e2e/full-journey.spec.ts` |
| `views/wallets/WalletsView.vue` | `e2e/wallets.spec.ts` |
| `views/transactions/TransactionsView.vue` | `e2e/transactions.spec.ts` |
| `views/budgets/BudgetsView.vue` | `e2e/budgets.spec.ts` |
| `views/goals/GoalsView.vue` | `e2e/goals.spec.ts` |
| `views/recurring/RecurringView.vue` | `e2e/recurring.spec.ts` |
| `views/settings/SettingsView.vue` | `e2e/settings.spec.ts` |
| `views/onboarding/OnboardingView.vue` | `e2e/onboarding.spec.ts` |
