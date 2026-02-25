import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import GoalMilestoneIndicator from "@/components/goals/GoalMilestoneIndicator.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountIndicator(lastMilestone: number, targetAmount: number, currentProgress: number) {
    return mount(GoalMilestoneIndicator, {
        props: { lastMilestone, targetAmount, currentProgress },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("GoalMilestoneIndicator", () => {
    it("renders exactly 4 milestone dots", () => {
        const wrapper = mountIndicator(0, 5000, 0);
        // Each milestone has a small dot div with h-3 w-3 rounded-full
        const dots = wrapper.findAll(".h-3.w-3.rounded-full");
        expect(dots).toHaveLength(4);
    });

    it("shows no green dots when lastMilestone is 0", () => {
        const wrapper = mountIndicator(0, 5000, 0);
        expect(wrapper.findAll(".bg-green-500")).toHaveLength(0);
    });

    it("shows 1 green dot when lastMilestone is 25", () => {
        const wrapper = mountIndicator(25, 5000, 1250);
        expect(wrapper.findAll(".bg-green-500")).toHaveLength(1);
    });

    it("shows 2 green dots when lastMilestone is 50", () => {
        const wrapper = mountIndicator(50, 5000, 2500);
        expect(wrapper.findAll(".bg-green-500")).toHaveLength(2);
    });

    it("shows 3 green dots when lastMilestone is 75", () => {
        const wrapper = mountIndicator(75, 5000, 3750);
        expect(wrapper.findAll(".bg-green-500")).toHaveLength(3);
    });

    it("shows all 4 green dots when lastMilestone is 100", () => {
        const wrapper = mountIndicator(100, 5000, 5000);
        expect(wrapper.findAll(".bg-green-500")).toHaveLength(4);
    });

    it("renders the 4 percentage labels", () => {
        const wrapper = mountIndicator(0, 5000, 0);
        expect(wrapper.text()).toContain("25%");
        expect(wrapper.text()).toContain("50%");
        expect(wrapper.text()).toContain("75%");
        expect(wrapper.text()).toContain("100%");
    });
});
