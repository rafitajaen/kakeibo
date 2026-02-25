import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BudgetForm from "@/components/budgets/BudgetForm.vue";
import type { Budget } from "@/stores/budgets";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const mockCategories: Category[] = [
    { id: "cat-1", name: "Housing", isSystem: true, isArchived: false },
    { id: "cat-2", name: "Food & Dining", isSystem: true, isArchived: false },
];

const mockWallets: Wallet[] = [
    {
        id: "wallet-1",
        name: "Checking",
        type: "Personal",
        currency: "EUR",
        balance: 1000,
        isArchived: false,
        createdAt: "2026-01-01T00:00:00Z",
    },
];

const mockBudget: Budget = {
    id: "budget-1",
    name: "Food Budget",
    categoryId: "cat-1",
    categoryName: "Housing",
    limit: 400,
    startDate: "2026-01-01",
    endDate: "2026-01-31",
    walletId: null,
    walletName: null,
    currentSpending: 100,
    createdAt: "2026-01-01T00:00:00Z",
};

function mountForm(props: { budget?: Budget; isSubmitting?: boolean }) {
    return mount(BudgetForm, {
        props: {
            budget: props.budget,
            categories: mockCategories,
            wallets: mockWallets,
            isSubmitting: props.isSubmitting,
        },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub reka-ui Select components to avoid portal/jsdom issues
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

describe("BudgetForm", () => {
    it("renders name, limit, startDate, and endDate input fields", () => {
        const wrapper = mountForm({});
        const inputs = wrapper.findAll("input");
        // name (text), limit (number), startDate (date), endDate (date)
        expect(inputs.length).toBeGreaterThanOrEqual(3);
    });

    it("shows 'Create budget' button in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Create budget");
    });

    it("shows 'Save changes' button in edit mode when budget is provided", () => {
        const wrapper = mountForm({ budget: mockBudget });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("pre-fills the name input in edit mode", () => {
        const wrapper = mountForm({ budget: mockBudget });
        const nameInput = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(nameInput.element.value).toBe("Food Budget");
    });

    it("pre-fills the limit input in edit mode", () => {
        const wrapper = mountForm({ budget: mockBudget });
        const limitInput = wrapper.find<HTMLInputElement>('input[type="number"]');
        expect(Number(limitInput.element.value)).toBe(400);
    });

    it("disables the submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeDefined();
    });
});
