import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import BudgetSummary from "@/components/dashboard/BudgetSummary.vue";
import type { Budget } from "@/stores/budgets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        {
            path: "/budgets/:id/edit",
            name: "budget-edit",
            component: { template: "<div/>" },
        },
        {
            path: "/budgets/new",
            name: "budget-create",
            component: { template: "<div/>" },
        },
    ],
});

const makeBudget = (overrides: Partial<Budget> = {}): Budget => ({
    id: "b-1",
    name: "Food Budget",
    categoryId: "cat-1",
    categoryName: "Food & Dining",
    limit: 400,
    startDate: "2026-01-01",
    endDate: "2026-01-31",
    walletId: null,
    walletName: null,
    currentSpending: 200,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountComponent(budgets: Budget[], isLoading = false) {
    return mount(BudgetSummary, {
        props: { budgets, isLoading },
        global: { plugins: [i18n, createPinia(), router] },
    });
}

describe("BudgetSummary", () => {
    it("shows skeleton placeholders when isLoading is true", () => {
        const wrapper = mountComponent([], true);
        expect(wrapper.findAll('[data-slot="skeleton"]').length).toBeGreaterThan(0);
    });

    it("shows empty state with create link when no budgets", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("No active budgets");
        expect(wrapper.text()).toContain("Create budget");
    });

    it("renders budget name and percentage", () => {
        const wrapper = mountComponent([makeBudget()]);
        expect(wrapper.text()).toContain("Food Budget");
        expect(wrapper.text()).toContain("50%");
    });

    it("renders progress bar", () => {
        const wrapper = mountComponent([makeBudget()]);
        expect(wrapper.find('[role="progressbar"]').exists()).toBe(true);
    });

    it("renders budget summary title", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("Budget Summary");
    });
});
