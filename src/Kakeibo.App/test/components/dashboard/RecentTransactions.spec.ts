import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import RecentTransactions from "@/components/dashboard/RecentTransactions.vue";
import type { Transaction } from "@/stores/transactions";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        {
            path: "/wallets/:walletId/transactions",
            name: "transactions",
            component: { template: "<div/>" },
        },
    ],
});

const makeTx = (overrides: Partial<Transaction> = {}): Transaction => ({
    id: "tx-1",
    type: "Expense",
    amount: 50,
    description: "Coffee",
    date: "2026-01-10",
    categoryId: "cat-1",
    categoryName: "Food & Dining",
    walletId: "w-1",
    destinationWalletId: null,
    createdAt: "2026-01-10T08:00:00Z",
    ...overrides,
});

function mountComponent(transactions: Transaction[], isLoading = false) {
    return mount(RecentTransactions, {
        props: { transactions, isLoading },
        global: { plugins: [i18n, createPinia(), router] },
    });
}

describe("RecentTransactions", () => {
    it("shows loading when isLoading is true", () => {
        const wrapper = mountComponent([], true);
        expect(wrapper.text()).toContain("Loading...");
    });

    it("shows empty state when no transactions", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("No recent transactions");
    });

    it("renders transaction description and category", () => {
        const wrapper = mountComponent([makeTx()]);
        expect(wrapper.text()).toContain("Coffee");
        expect(wrapper.text()).toContain("Food & Dining");
    });

    it("renders income transaction with + prefix", () => {
        const wrapper = mountComponent([makeTx({ type: "Income", amount: 200 })]);
        expect(wrapper.text()).toContain("+200.00");
    });

    it("renders expense transaction with - prefix", () => {
        const wrapper = mountComponent([makeTx({ type: "Expense", amount: 50 })]);
        expect(wrapper.text()).toContain("-50.00");
    });

    it("renders recent transactions title", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("Recent Transactions");
    });
});
