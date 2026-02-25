import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import GoalProgressBar from "@/components/goals/GoalProgressBar.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBar(currentProgress: number, targetAmount: number) {
    return mount(GoalProgressBar, {
        props: { currentProgress, targetAmount },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("GoalProgressBar", () => {
    it("renders the progress element with correct ARIA role", () => {
        const wrapper = mountBar(500, 5000);
        expect(wrapper.find('[role="progressbar"]').exists()).toBe(true);
    });

    it("applies slate color when progress is below 50%", () => {
        const wrapper = mountBar(49, 100);
        expect(wrapper.html()).toContain("bg-slate-400");
    });

    it("applies blue-400 color when progress is exactly 50%", () => {
        const wrapper = mountBar(50, 100);
        expect(wrapper.html()).toContain("bg-blue-400");
    });

    it("applies blue-500 color when progress is between 75% and 99%", () => {
        const wrapper = mountBar(75, 100);
        expect(wrapper.html()).toContain("bg-blue-500");
    });

    it("applies green color when progress equals 100%", () => {
        const wrapper = mountBar(100, 100);
        expect(wrapper.html()).toContain("bg-green-500");
    });

    it("applies green color when progress exceeds target", () => {
        const wrapper = mountBar(120, 100);
        expect(wrapper.html()).toContain("bg-green-500");
    });

    it("clamps aria-valuenow at 100 when progress exceeds target", () => {
        const wrapper = mountBar(200, 100);
        const progressbar = wrapper.find('[role="progressbar"]');
        expect(Number(progressbar.attributes("aria-valuenow"))).toBeLessThanOrEqual(100);
    });

    it("sets width style proportional to progress percentage", () => {
        const wrapper = mountBar(50, 100);
        const bar = wrapper.find('[role="progressbar"]');
        expect(bar.attributes("style")).toContain("width: 50%");
    });

    it("sets width style to 0% when progress is 0", () => {
        const wrapper = mountBar(0, 100);
        const bar = wrapper.find('[role="progressbar"]');
        expect(bar.attributes("style")).toContain("width: 0%");
    });
});
