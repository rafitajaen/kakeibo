import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import WalletList from "@/components/wallets/WalletList.vue";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const wallet1: Wallet = {
    id: "w-1",
    name: "Checking Account",
    type: "Personal",
    currency: "EUR",
    balance: 100,
    isArchived: false,
    createdAt: "2026-03-01T00:00:00Z",
};

const wallet2: Wallet = {
    id: "w-2",
    name: "Savings",
    type: "Personal",
    currency: "EUR",
    balance: 500,
    isArchived: false,
    createdAt: "2026-03-02T00:00:00Z",
};

function mountList(wallets: Wallet[], emptyMessage = "No wallets yet.") {
    return mount(WalletList, {
        props: { wallets, emptyMessage },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub WalletCard to avoid deep rendering and router dependencies
                WalletCard: {
                    template: '<div class="wallet-card">{{ wallet.name }}</div>',
                    props: ["wallet"],
                },
            },
        },
    });
}

describe("WalletList", () => {
    it("shows the empty message when there are no wallets", () => {
        const wrapper = mountList([], "No wallets here.");
        expect(wrapper.text()).toContain("No wallets here.");
    });

    it("does not show the empty message when wallets are present", () => {
        const wrapper = mountList([wallet1]);
        expect(wrapper.text()).not.toContain("No wallets yet.");
    });

    it("renders a card for each wallet", () => {
        const wrapper = mountList([wallet1, wallet2]);
        const cards = wrapper.findAll(".wallet-card");
        expect(cards).toHaveLength(2);
    });

    it("renders the wallet names via WalletCard", () => {
        const wrapper = mountList([wallet1, wallet2]);
        expect(wrapper.text()).toContain("Checking Account");
        expect(wrapper.text()).toContain("Savings");
    });
});
