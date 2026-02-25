import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BudgetList from "@/components/budgets/BudgetList.vue";
import type { Budget } from "@/stores/budgets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeBudget = (overrides: Partial<Budget> = {}): Budget => ({
    id: "budget-1",
    name: "Food Budget",
    categoryId: "cat-1",
    categoryName: "Food & Dining",
    limit: 400,
    startDate: "2026-01-01",
    endDate: "2026-01-31",
    walletId: null,
    walletName: null,
    currentSpending: 100,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountList(budgets: Budget[], isLoading = false) {
    return mount(BudgetList, {
        props: { budgets, isLoading },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("BudgetList", () => {
    it("shows loading message when isLoading is true", () => {
        const wrapper = mountList([], true);
        expect(wrapper.text()).toContain("Loading...");
    });

    it("shows empty state message when list is empty", () => {
        const wrapper = mountList([]);
        expect(wrapper.text()).toContain(
            "No budgets yet. Create one to start tracking your spending.",
        );
    });

    it("renders budget name and category", () => {
        const wrapper = mountList([makeBudget()]);
        expect(wrapper.text()).toContain("Food Budget");
        expect(wrapper.text()).toContain("Food & Dining");
    });

    it("renders spending vs limit text", () => {
        const wrapper = mountList([makeBudget({ currentSpending: 100, limit: 400 })]);
        expect(wrapper.text()).toContain("100.00");
        expect(wrapper.text()).toContain("400.00");
    });

    it("shows period dates", () => {
        const wrapper = mountList([makeBudget()]);
        expect(wrapper.text()).toContain("2026-01-01");
        expect(wrapper.text()).toContain("2026-01-31");
    });

    it("emits edit event when edit button is clicked", async () => {
        const wrapper = mountList([makeBudget()]);
        const buttons = wrapper.findAll("button");
        const editButton = buttons.find((b) => b.text() === "Edit");
        await editButton?.trigger("click");
        expect(wrapper.emitted("edit")).toBeTruthy();
        expect(wrapper.emitted("edit")?.[0]?.[0]).toBe("budget-1");
    });

    it("emits delete event when delete button is clicked", async () => {
        const wrapper = mountList([makeBudget()]);
        const buttons = wrapper.findAll("button");
        const deleteButton = buttons.find((b) => b.text() === "Delete");
        await deleteButton?.trigger("click");
        expect(wrapper.emitted("delete")).toBeTruthy();
        expect(wrapper.emitted("delete")?.[0]?.[0]).toBe("budget-1");
    });
});
