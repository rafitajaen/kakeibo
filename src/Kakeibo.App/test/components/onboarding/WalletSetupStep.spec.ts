import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { createPinia, setActivePinia } from "pinia";
import WalletSetupStep from "@/components/onboarding/WalletSetupStep.vue";

vi.mock("@/lib/axios");

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            onboarding: {
                walletSetup: {
                    title: "Create your first wallet",
                    description: "Start by adding a wallet.",
                    walletName: "Wallet name",
                    walletNamePlaceholder: "e.g. Checking Account",
                    create: "Create Wallet",
                    skip: "I'll do this later",
                },
            },
            common: {
                back: "Back",
                errorGeneric: "Something went wrong.",
            },
        },
    },
});

describe("WalletSetupStep", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    it("renders title and wallet name input", () => {
        const wrapper = mount(WalletSetupStep, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Create your first wallet");
        expect(wrapper.find("input").exists()).toBe(true);
    });

    it("emits 'back' when Back is clicked", async () => {
        const wrapper = mount(WalletSetupStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const backBtn = buttons.find((b) => b.text() === "Back");
        await backBtn!.trigger("click");
        expect(wrapper.emitted("back")).toBeTruthy();
    });

    it("emits 'skip' when skip link is clicked", async () => {
        const wrapper = mount(WalletSetupStep, { global: { plugins: [i18n] } });
        const buttons = wrapper.findAll("button");
        const skipBtn = buttons.find((b) => b.text() === "I'll do this later");
        await skipBtn!.trigger("click");
        expect(wrapper.emitted("skip")).toBeTruthy();
    });

    it("shows create button as submit", () => {
        const wrapper = mount(WalletSetupStep, { global: { plugins: [i18n] } });
        const submitBtn = wrapper.find("button[type='submit']");
        expect(submitBtn.exists()).toBe(true);
        expect(submitBtn.text()).toBe("Create Wallet");
    });
});
