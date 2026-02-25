import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BudgetProgressBar from "@/components/budgets/BudgetProgressBar.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBar(currentSpending: number, limit: number) {
    return mount(BudgetProgressBar, {
        props: { currentSpending, limit },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("BudgetProgressBar", () => {
    it("renders the progress element", () => {
        const wrapper = mountBar(50, 100);
        // shadcn Progress renders a div with role="progressbar"
        expect(wrapper.find('[role="progressbar"]').exists()).toBe(true);
    });

    it("applies green color class when spending is below 75% of limit", () => {
        const wrapper = mountBar(74, 100);
        expect(wrapper.html()).toContain("bg-green-500");
    });

    it("applies yellow color class when spending is between 75% and 99% of limit", () => {
        const wrapper = mountBar(75, 100);
        expect(wrapper.html()).toContain("bg-yellow-500");
    });

    it("applies destructive color class when spending is at or above 100% of limit", () => {
        const wrapper = mountBar(100, 100);
        expect(wrapper.html()).toContain("bg-destructive");
    });

    it("clamps percentage at 100 when spending exceeds the limit", () => {
        const wrapper = mountBar(200, 100);
        const progressbar = wrapper.find('[role="progressbar"]');
        // aria-valuenow should be capped at 100
        expect(Number(progressbar.attributes("aria-valuenow"))).toBeLessThanOrEqual(100);
    });
});
