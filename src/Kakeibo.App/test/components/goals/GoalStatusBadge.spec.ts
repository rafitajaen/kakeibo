import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import GoalStatusBadge from "@/components/goals/GoalStatusBadge.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBadge(currentProgress: number, targetAmount: number) {
    return mount(GoalStatusBadge, {
        props: { currentProgress, targetAmount },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("GoalStatusBadge", () => {
    it("shows 'Not started' when progress is 0", () => {
        const wrapper = mountBadge(0, 5000);
        expect(wrapper.text()).toContain("Not started");
    });

    it("shows 'In progress' when progress is between 1% and 74%", () => {
        const wrapper = mountBadge(500, 5000); // 10%
        expect(wrapper.text()).toContain("In progress");
    });

    it("shows 'Near target' when progress is between 75% and 99%", () => {
        const wrapper = mountBadge(4000, 5000); // 80%
        expect(wrapper.text()).toContain("Near target");
    });

    it("shows 'Achieved' when progress equals target", () => {
        const wrapper = mountBadge(5000, 5000); // 100%
        expect(wrapper.text()).toContain("Achieved");
    });

    it("shows 'Achieved' when progress exceeds target", () => {
        const wrapper = mountBadge(6000, 5000); // 120%
        expect(wrapper.text()).toContain("Achieved");
    });

    it("shows 'In progress' at exactly 74% of target", () => {
        const wrapper = mountBadge(74, 100);
        expect(wrapper.text()).toContain("In progress");
    });

    it("shows 'Near target' at exactly 75% of target", () => {
        const wrapper = mountBadge(75, 100);
        expect(wrapper.text()).toContain("Near target");
    });
});
