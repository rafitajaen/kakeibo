import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import WelcomeStep from "@/components/onboarding/WelcomeStep.vue";

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            onboarding: {
                welcome: {
                    title: "Welcome to Kakeibo",
                    subtitle: "Mindful money management",
                    description: "Track your finances.",
                    getStarted: "Get Started",
                    skip: "Skip",
                },
            },
        },
    },
});

describe("WelcomeStep", () => {
    it("renders title and description", () => {
        const wrapper = mount(WelcomeStep, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Welcome to Kakeibo");
        expect(wrapper.text()).toContain("Track your finances.");
    });

    it("emits 'next' when Get Started is clicked", async () => {
        const wrapper = mount(WelcomeStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const getStartedBtn = buttons.find((b) => b.text() === "Get Started");
        await getStartedBtn!.trigger("click");
        expect(wrapper.emitted("next")).toBeTruthy();
    });

    it("emits 'skip' when Skip is clicked", async () => {
        const wrapper = mount(WelcomeStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const skipBtn = buttons.find((b) => b.text() === "Skip");
        await skipBtn!.trigger("click");
        expect(wrapper.emitted("skip")).toBeTruthy();
    });

    it("shows emoji icon", () => {
        const wrapper = mount(WelcomeStep, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("🏮");
    });
});
