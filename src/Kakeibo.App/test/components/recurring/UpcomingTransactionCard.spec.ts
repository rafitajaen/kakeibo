import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import UpcomingTransactionCard from "@/components/recurring/UpcomingTransactionCard.vue";
import type { ForecastItem } from "@/stores/recurring";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeItem = (overrides: Partial<ForecastItem> = {}): ForecastItem => ({
    patternId: "pattern-1",
    patternName: "Monthly Rent",
    transactionType: "Expense",
    amount: 1200,
    categoryName: "Housing",
    walletName: "Checking",
    date: "2026-04-01",
    frequency: "Monthly",
    ...overrides,
});

function mountCard(item: ForecastItem) {
    return mount(UpcomingTransactionCard, {
        props: { item },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("UpcomingTransactionCard", () => {
    it("renders pattern name", () => {
        const wrapper = mountCard(makeItem({ patternName: "Monthly Rent" }));
        expect(wrapper.text()).toContain("Monthly Rent");
    });

    it("renders formatted date", () => {
        const wrapper = mountCard(makeItem({ date: "2026-04-01" }));
        // date-fns 'PP' format: Apr 1, 2026
        expect(wrapper.text()).toContain("Apr 1, 2026");
    });

    it("renders amount formatted to 2 decimal places", () => {
        const wrapper = mountCard(makeItem({ amount: 1200 }));
        expect(wrapper.text()).toContain("1200.00");
    });

    it("shows minus sign for expense transactions", () => {
        const wrapper = mountCard(makeItem({ transactionType: "Expense", amount: 50 }));
        expect(wrapper.text()).toContain("-50.00");
    });

    it("shows plus sign for income transactions", () => {
        const wrapper = mountCard(makeItem({ transactionType: "Income", amount: 2000 }));
        expect(wrapper.text()).toContain("+2000.00");
    });

    it("shows no sign prefix for transfer transactions", () => {
        const wrapper = mountCard(makeItem({ transactionType: "Transfer", amount: 500 }));
        const text = wrapper.text();
        expect(text).toContain("500.00");
        expect(text).not.toContain("+500.00");
        expect(text).not.toContain("-500.00");
    });

    it("renders category and wallet name", () => {
        const wrapper = mountCard(makeItem({ categoryName: "Housing", walletName: "Savings" }));
        expect(wrapper.text()).toContain("Housing");
        expect(wrapper.text()).toContain("Savings");
    });
});
