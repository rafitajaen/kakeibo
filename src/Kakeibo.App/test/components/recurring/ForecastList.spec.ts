import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import ForecastList from "@/components/recurring/ForecastList.vue";
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

function mountList(items: ForecastItem[]) {
    return mount(ForecastList, {
        props: { items },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("ForecastList", () => {
    it("shows empty message when no items", () => {
        const wrapper = mountList([]);
        expect(wrapper.text()).toContain("No upcoming transactions");
    });

    it("renders all forecast items", () => {
        const items = [
            makeItem({ patternName: "Rent", date: "2026-04-01" }),
            makeItem({ patternId: "pattern-2", patternName: "Gym", date: "2026-04-10" }),
        ];
        const wrapper = mountList(items);
        expect(wrapper.findAll('[data-testid="forecast-item"]')).toHaveLength(2);
    });

    it("renders pattern name for each item", () => {
        const wrapper = mountList([makeItem({ patternName: "Monthly Rent" })]);
        expect(wrapper.text()).toContain("Monthly Rent");
    });

    it("does not show empty message when items are present", () => {
        const wrapper = mountList([makeItem()]);
        expect(wrapper.text()).not.toContain("No upcoming transactions");
    });
});
