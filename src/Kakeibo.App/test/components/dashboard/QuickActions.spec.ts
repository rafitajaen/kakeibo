import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import QuickActions from "@/components/dashboard/QuickActions.vue";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        {
            path: "/wallets/new",
            name: "wallets-create",
            component: { template: "<div/>" },
        },
        {
            path: "/wallets/:walletId/transactions/new",
            name: "transaction-record",
            component: { template: "<div/>" },
        },
    ],
});

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

function mountComponent(wallets: Wallet[]) {
    return mount(QuickActions, {
        props: { wallets },
        global: { plugins: [i18n, createPinia(), router] },
    });
}

describe("QuickActions", () => {
    it("renders both buttons", () => {
        const wrapper = mountComponent([makeWallet()]);
        expect(wrapper.text()).toContain("Record Transaction");
        expect(wrapper.text()).toContain("Create Wallet");
    });

    it("record button is disabled when no wallets", () => {
        const wrapper = mountComponent([]);
        const buttons = wrapper.findAll("button");
        const recordBtn = buttons.find((b) => b.text() === "Record Transaction");
        expect(recordBtn?.attributes("disabled")).toBeDefined();
    });

    it("record button is enabled when wallets exist", () => {
        const wrapper = mountComponent([makeWallet()]);
        const buttons = wrapper.findAll("button");
        const recordBtn = buttons.find((b) => b.text() === "Record Transaction");
        expect(recordBtn?.attributes("disabled")).toBeUndefined();
    });

    it("create wallet button is always enabled", () => {
        const wrapper = mountComponent([]);
        const buttons = wrapper.findAll("button");
        const createBtn = buttons.find((b) => b.text() === "Create Wallet");
        expect(createBtn?.attributes("disabled")).toBeUndefined();
    });
});
