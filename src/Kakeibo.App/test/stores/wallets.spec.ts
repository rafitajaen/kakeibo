import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useWalletsStore } from "@/stores/wallets";

// Mock the axios instance so no real HTTP requests are made.
vi.mock("@/lib/axios", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    },
}));

import api from "@/lib/axios";

const apiMock = api as unknown as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

const wallet1 = {
    id: "id-1",
    name: "Checking",
    type: "Personal",
    currency: "EUR",
    balance: 0,
    isArchived: false,
    createdAt: "2026-03-01T00:00:00Z",
};

const wallet2 = {
    id: "id-2",
    name: "Savings",
    type: "Personal",
    currency: "EUR",
    balance: 0,
    isArchived: true,
    createdAt: "2026-03-01T00:00:00Z",
};

describe("useWalletsStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("fetchWallets populates the wallets list", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [wallet1] });
        const store = useWalletsStore();

        await store.fetchWallets();

        expect(store.wallets).toHaveLength(1);
        expect(store.wallets[0].name).toBe("Checking");
        expect(store.isLoading).toBe(false);
    });

    it("fetchWallets passes includeArchived query param", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [wallet1, wallet2] });
        const store = useWalletsStore();

        await store.fetchWallets(true);

        expect(apiMock.get).toHaveBeenCalledWith("/api/wallets", {
            params: { includeArchived: true },
        });
        expect(store.wallets).toHaveLength(2);
    });

    it("personalWallets filters only non-archived Personal wallets", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [wallet1, wallet2] });
        const store = useWalletsStore();
        await store.fetchWallets(true);

        expect(store.personalWallets).toHaveLength(1);
        expect(store.personalWallets[0].id).toBe("id-1");
    });

    it("archivedWallets filters only archived wallets", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [wallet1, wallet2] });
        const store = useWalletsStore();
        await store.fetchWallets(true);

        expect(store.archivedWallets).toHaveLength(1);
        expect(store.archivedWallets[0].id).toBe("id-2");
    });

    it("fetchWallet sets currentWallet", async () => {
        apiMock.get.mockResolvedValueOnce({ data: wallet1 });
        const store = useWalletsStore();

        await store.fetchWallet("id-1");

        expect(store.currentWallet).toEqual(wallet1);
        expect(store.isLoading).toBe(false);
    });

    it("createWallet adds the new wallet to the list", async () => {
        apiMock.post.mockResolvedValueOnce({ data: wallet1 });
        const store = useWalletsStore();

        const result = await store.createWallet({ name: "Checking", type: "Personal" });

        expect(result).toEqual(wallet1);
        expect(store.wallets).toHaveLength(1);
        expect(apiMock.post).toHaveBeenCalledWith("/api/wallets", {
            name: "Checking",
            type: "Personal",
        });
    });

    it("updateWallet replaces the wallet in the list", async () => {
        const updated = { ...wallet1, name: "Main Checking" };
        apiMock.get.mockResolvedValueOnce({ data: [wallet1] });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useWalletsStore();
        await store.fetchWallets();

        await store.updateWallet("id-1", { name: "Main Checking" });

        expect(store.wallets[0].name).toBe("Main Checking");
    });

    it("updateWallet also refreshes currentWallet when it matches", async () => {
        const updated = { ...wallet1, name: "Main Checking" };
        apiMock.get.mockResolvedValueOnce({ data: wallet1 });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useWalletsStore();
        await store.fetchWallet("id-1");

        await store.updateWallet("id-1", { name: "Main Checking" });

        expect(store.currentWallet?.name).toBe("Main Checking");
    });

    it("archiveWallet removes the wallet from the list", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [wallet1] });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useWalletsStore();
        await store.fetchWallets();

        await store.archiveWallet("id-1");

        expect(store.wallets).toHaveLength(0);
        expect(apiMock.delete).toHaveBeenCalledWith("/api/wallets/id-1");
    });

    it("archiveWallet clears currentWallet when it matches", async () => {
        apiMock.get.mockResolvedValueOnce({ data: wallet1 });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useWalletsStore();
        await store.fetchWallet("id-1");

        await store.archiveWallet("id-1");

        expect(store.currentWallet).toBeNull();
    });
});
