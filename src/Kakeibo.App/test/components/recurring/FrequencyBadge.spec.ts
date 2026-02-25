import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import FrequencyBadge from "@/components/recurring/FrequencyBadge.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBadge(frequency: string) {
    return mount(FrequencyBadge, {
        props: { frequency },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("FrequencyBadge", () => {
    it("shows 'Daily' for daily frequency", () => {
        const wrapper = mountBadge("Daily");
        expect(wrapper.text()).toContain("Daily");
    });

    it("shows 'Weekly' for weekly frequency", () => {
        const wrapper = mountBadge("Weekly");
        expect(wrapper.text()).toContain("Weekly");
    });

    it("shows 'Biweekly' for biweekly frequency", () => {
        const wrapper = mountBadge("Biweekly");
        expect(wrapper.text()).toContain("Biweekly");
    });

    it("shows 'Monthly' for monthly frequency", () => {
        const wrapper = mountBadge("Monthly");
        expect(wrapper.text()).toContain("Monthly");
    });

    it("shows 'Yearly' for yearly frequency", () => {
        const wrapper = mountBadge("Yearly");
        expect(wrapper.text()).toContain("Yearly");
    });

    it("renders a badge element", () => {
        const wrapper = mountBadge("Monthly");
        expect(wrapper.find("div").exists()).toBe(true);
    });
});
