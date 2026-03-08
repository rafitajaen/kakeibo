# 04 — Testear Componentes Vue

---

## Qué testear en un componente

- **Renderizado correcto**: los textos, labels y elementos clave aparecen según las props
- **Comportamiento condicional**: `v-if`, `v-show`, clases dinámicas
- **Formularios**: campos pre-rellenados en modo edición, botón deshabilitado cuando se envía
- **Eventos emitidos**: que el componente emite el evento correcto con el payload correcto
- **Interacción**: qué pasa cuando el usuario hace clic o escribe

## Qué NO testear

- **CSS/estilos visuales**: si una clase existe o no (frágil, no aporta valor)
- **Internals de shadcn-vue**: no testees que un `<Dialog>` abre o que un `<Select>` muestra opciones
- **La librería Vue en sí**: no testees que `v-model` funciona

---

## Setup estándar: mount helper

Cada archivo de test de componente define un helper `mountXxx()` que configura
los plugins necesarios una sola vez. Esto evita repetir la configuración en cada `it`.

```typescript
import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BudgetForm from "@/components/budgets/BudgetForm.vue";
import type { Budget } from "@/stores/budgets";
import type { Category } from "@/stores/categories";
import en from "@/locales/en.json";

// i18n con mensajes reales del proyecto (no mocks)
const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

// Datos de prueba tipados
const mockCategories: Category[] = [
    { id: "cat-1", name: "Housing", isSystem: true, isArchived: false, isPrivate: false },
    { id: "cat-2", name: "Food & Dining", isSystem: true, isArchived: false, isPrivate: false },
];

// Helper central: único lugar donde se configura el montaje
function mountForm(props: { budget?: Budget; isSubmitting?: boolean }) {
    return mount(BudgetForm, {
        props: {
            budget: props.budget,
            categories: mockCategories,
            isSubmitting: props.isSubmitting ?? false,
        },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub de Select porque jsdom no soporta portales de Radix UI
                Select: {
                    template: '<div class="select-stub"><slot /></div>',
                    props: ["modelValue"],
                    emits: ["update:modelValue"],
                },
                SelectTrigger: { template: "<div />" },
                SelectContent: { template: "<div><slot /></div>" },
                SelectItem: { template: "<div />", props: ["value"] },
                SelectValue: { template: "<div />" },
            },
        },
    });
}
```

---

## Cómo mockear un store en un componente

Cuando el componente inyecta un store internamente, usa `setActivePinia` y
después accede al store para pre-popular su estado:

```typescript
import { setActivePinia, createPinia } from "pinia";
import { useWalletsStore } from "@/stores/wallets";

beforeEach(() => {
    setActivePinia(createPinia());
});

it("shows the wallet name from the store", () => {
    const walletsStore = useWalletsStore();
    // Mutar estado del store directamente (no hace llamada HTTP)
    walletsStore.wallets = [
        { id: "w-1", name: "Checking", type: "Personal", currency: "EUR",
          balance: 500, isArchived: false, createdAt: "2026-01-01T00:00:00Z" },
    ];

    const wrapper = mount(WalletSelector, {
        global: { plugins: [i18n, createPinia()] },
    });

    expect(wrapper.text()).toContain("Checking");
});
```

**Nota:** Cuando el componente llama a `store.fetchXxx()` en `onMounted`, también
necesitas mockear axios. Combina el setup de stores con el setup de axios del documento
[03-stores.md](./03-stores.md).

---

## Testear renderizado y props

```typescript
describe("BudgetForm", () => {
    it("renders name and limit input fields", () => {
        const wrapper = mountForm({});
        const inputs = wrapper.findAll("input");
        expect(inputs.length).toBeGreaterThanOrEqual(2);
    });

    it("shows 'Create budget' button in create mode (no budget prop)", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Create budget");
    });

    it("shows 'Save changes' button in edit mode (budget prop provided)", () => {
        const wrapper = mountForm({ budget: makeBudget() });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("pre-fills the name input with the existing budget name", () => {
        const wrapper = mountForm({ budget: makeBudget({ name: "Rent Budget" }) });
        const nameInput = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(nameInput.element.value).toBe("Rent Budget");
    });
});
```

---

## Testear comportamiento condicional

```typescript
it("disables the submit button while isSubmitting is true", () => {
    const wrapper = mountForm({ isSubmitting: true });
    const button = wrapper.find('button[type="submit"]');
    expect(button.attributes("disabled")).toBeDefined();
});

it("submit button is enabled when isSubmitting is false", () => {
    const wrapper = mountForm({ isSubmitting: false });
    const button = wrapper.find('button[type="submit"]');
    expect(button.attributes("disabled")).toBeUndefined();
});

it("shows error state badge when budget is exceeded", () => {
    const wrapper = mount(BudgetStatusBadge, {
        props: { currentSpending: 450, limit: 400 },
        global: { plugins: [i18n] },
    });
    expect(wrapper.text()).toContain("Exceeded");
});
```

---

## Testear formularios (VeeValidate + Zod)

Los formularios con VeeValidate no se pueden testear fácilmente con validación real
porque necesitan el ciclo completo de submit. En cambio, testea:

1. Que los campos se renderizan y pre-rellenan
2. Que el botón de submit está deshabilitado cuando `isSubmitting` es `true`
3. Que el componente emite el evento correcto al hacer submit

```typescript
it("emits 'submit' event with form data when the form is submitted", async () => {
    const wrapper = mountForm({});
    const nameInput = wrapper.find<HTMLInputElement>('input[name="name"]');
    await nameInput.setValue("My Budget");

    await wrapper.find("form").trigger("submit");

    // El componente emite el evento hacia el padre
    expect(wrapper.emitted("submit")).toBeTruthy();
});
```

---

## Testear eventos emitidos

```typescript
it("emits 'cancel' when the cancel button is clicked", async () => {
    const wrapper = mountForm({ budget: makeBudget() });
    const cancelButton = wrapper.find('button[data-testid="cancel"]');

    await cancelButton.trigger("click");

    expect(wrapper.emitted("cancel")).toHaveLength(1);
});

it("emits 'delete' with the budget id when delete is confirmed", async () => {
    const wrapper = mountForm({ budget: makeBudget({ id: "budget-abc" }) });

    await wrapper.find('[data-testid="delete-btn"]').trigger("click");

    expect(wrapper.emitted("delete")).toEqual([["budget-abc"]]);
});
```

---

## Cuándo usar `mount` vs `shallowMount`

| Situación | Usa |
|-----------|-----|
| Necesitas verificar el texto renderizado en subcomponentes | `mount` |
| El componente tiene muchas dependencias externas (otros stores, portales) | `shallowMount` |
| Estás testeando la lógica del componente, no su integración | `shallowMount` |
| Necesitas interactuar con inputs o botones dentro del componente | `mount` |

En este proyecto usamos `mount` con **stubs específicos** para los componentes
de shadcn-vue que causan problemas en jsdom (portales, Radix UI). Es más fiable
que `shallowMount` global.

---

## Por qué hacemos stub de los Select de shadcn-vue

jsdom no implementa correctamente los portales del DOM que usa Radix UI (el sistema
de primitivos detrás de shadcn-vue). Cuando montamos un `<Select>` completo, lanza
errores o renderiza en el lugar incorrecto del DOM.

La solución es un stub minimal que:
- Renderiza su slot (para que el contenido sea visible)
- Acepta y emite `modelValue` (para que v-model funcione)
- No intenta crear portales

```typescript
stubs: {
    Select: {
        template: '<div class="select-stub"><slot /></div>',
        props: ["modelValue"],
        emits: ["update:modelValue"],
    },
    SelectTrigger: { template: "<div />" },
    SelectContent: { template: "<div><slot /></div>" },
    SelectItem: { template: "<div />", props: ["value"] },
    SelectValue: { template: "<div />" },
}
```

---

## Inventario de component tests existentes

| Dominio | Componentes con spec |
|---------|---------------------|
| `activity/` | Activity feed, activity item |
| `budgets/` | BudgetForm, BudgetList, BudgetProgressBar |
| `categories/` | CategoryForm, CategorySelector |
| `dashboard/` | SectionCards, ChartAreaInteractive |
| `goals/` | GoalForm, GoalList, GoalStatusBadge |
| `notifications/` | NotificationList, NotificationPreferences |
| `onboarding/` | OnboardingStep, OnboardingWizard |
| `recurring/` | FrequencyBadge, RecurringForm, RecurringList, ForecastList, ForecastCard |
| `settings/` | ProfileForm, SettingsPreferences |
| `transactions/` | TransactionForm, TransactionList |
| `wallets/` | WalletCard, MemberList |
