import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import BalanceOverview from "@/components/dashboard/BalanceOverview.vue";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeWallet = (overrides: Partial<Wallet> = {}): Wallet => ({
    id: "w-1",
    name: "Checking",
    type: "Personal",
    currency: "EUR",
    balance: 1000,
    isArchived: false,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountComponent(wallets: Wallet[], isLoading = false) {
    return mount(BalanceOverview, {
        props: { wallets, isLoading },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("BalanceOverview", () => {
    it("shows loading when isLoading is true", () => {
        const wrapper = mountComponent([], true);
        expect(wrapper.text()).toContain("Loading...");
    });

    it("shows empty state when no wallets", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("No wallets yet");
    });

    it("renders total balance for multiple wallets", () => {
        const wallets = [
            makeWallet({ balance: 500 }),
            makeWallet({ id: "w-2", name: "Savings", balance: 200 }),
        ];
        const wrapper = mountComponent(wallets);
        // Total: 500 + 200 = 700
        expect(wrapper.text()).toContain("700.00");
    });

    it("renders each wallet name and balance", () => {
        const wrapper = mountComponent([makeWallet()]);
        expect(wrapper.text()).toContain("Checking");
        expect(wrapper.text()).toContain("1000.00");
    });

    it("renders balance overview title", () => {
        const wrapper = mountComponent([makeWallet()]);
        expect(wrapper.text()).toContain("Balance Overview");
    });
});
