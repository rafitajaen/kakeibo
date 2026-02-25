import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import TourStep from "@/components/onboarding/TourStep.vue";

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            onboarding: {
                tour: {
                    title: "What you can do",
                    wallets: { title: "Wallets", description: "Manage accounts" },
                    transactions: { title: "Transactions", description: "Record events" },
                    budgets: { title: "Budgets", description: "Set limits" },
                    goals: { title: "Goals", description: "Save targets" },
                },
            },
            common: { back: "Back", next: "Next" },
        },
    },
});

describe("TourStep", () => {
    it("renders 4 feature cards", () => {
        const wrapper = mount(TourStep, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Wallets");
        expect(wrapper.text()).toContain("Transactions");
        expect(wrapper.text()).toContain("Budgets");
        expect(wrapper.text()).toContain("Goals");
    });

    it("renders the title", () => {
        const wrapper = mount(TourStep, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("What you can do");
    });

    it("emits 'next' when Next is clicked", async () => {
        const wrapper = mount(TourStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const nextBtn = buttons.find((b) => b.text() === "Next");
        await nextBtn!.trigger("click");
        expect(wrapper.emitted("next")).toBeTruthy();
    });

    it("emits 'back' when Back is clicked", async () => {
        const wrapper = mount(TourStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const backBtn = buttons.find((b) => b.text() === "Back");
        await backBtn!.trigger("click");
        expect(wrapper.emitted("back")).toBeTruthy();
    });
});
