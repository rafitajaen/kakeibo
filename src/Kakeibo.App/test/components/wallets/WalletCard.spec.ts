import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import WalletCard from "@/components/wallets/WalletCard.vue";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

// Mock the wallets store so no real HTTP requests are made.
vi.mock("@/stores/wallets", async (importOriginal) => {
    const actual = await importOriginal<typeof import("@/stores/wallets")>();
    return {
        ...actual,
        useWalletsStore: vi.fn(() => ({
            archiveWallet: vi.fn(),
        })),
    };
});

// Mock vue-router so router-push works without a full router setup.
vi.mock("vue-router", async (importOriginal) => {
    const actual = await importOriginal<typeof import("vue-router")>();
    return {
        ...actual,
        useRouter: vi.fn(() => ({ push: vi.fn() })),
    };
});

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const personalWallet: Wallet = {
    id: "w-1",
    name: "Checking Account",
    type: "Personal",
    currency: "EUR",
    balance: 150.5,
    isArchived: false,
    createdAt: "2026-03-01T00:00:00Z",
};

function mountCard(wallet: Wallet) {
    return mount(WalletCard, {
        props: { wallet },
        global: {
            plugins: [i18n, createPinia()],
        },
    });
}

describe("WalletCard", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders the wallet name", () => {
        const wrapper = mountCard(personalWallet);
        expect(wrapper.text()).toContain("Checking Account");
    });

    it("renders the currency", () => {
        const wrapper = mountCard(personalWallet);
        expect(wrapper.text()).toContain("EUR");
    });

    it("renders the formatted balance", () => {
        const wrapper = mountCard(personalWallet);
        expect(wrapper.text()).toContain("150.50");
    });

    it("renders the type badge with localized text", () => {
        const wrapper = mountCard(personalWallet);
        expect(wrapper.text()).toContain("Personal");
    });

    it("renders edit action button", () => {
        const wrapper = mountCard(personalWallet);
        expect(wrapper.text()).toContain("Edit");
    });
});
