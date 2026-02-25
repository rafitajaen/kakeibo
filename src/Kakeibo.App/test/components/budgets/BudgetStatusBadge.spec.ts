import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BudgetStatusBadge from "@/components/budgets/BudgetStatusBadge.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBadge(currentSpending: number, limit: number) {
    return mount(BudgetStatusBadge, {
        props: { currentSpending, limit },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("BudgetStatusBadge", () => {
    it("shows 'On track' when spending is below 75% of limit", () => {
        const wrapper = mountBadge(74, 100);
        expect(wrapper.text()).toContain("On track");
    });

    it("shows 'Warning' when spending is between 75% and 99% of limit", () => {
        const wrapper = mountBadge(75, 100);
        expect(wrapper.text()).toContain("Warning");
    });

    it("shows 'Exceeded' when spending is at or above 100% of limit", () => {
        const wrapper = mountBadge(100, 100);
        expect(wrapper.text()).toContain("Exceeded");
    });

    it("shows 'Exceeded' when spending exceeds the limit", () => {
        const wrapper = mountBadge(105, 100);
        expect(wrapper.text()).toContain("Exceeded");
    });

    it("shows 'On track' when spending is 0", () => {
        const wrapper = mountBadge(0, 100);
        expect(wrapper.text()).toContain("On track");
    });
});
