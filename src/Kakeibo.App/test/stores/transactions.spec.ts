import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useTransactionsStore } from "@/stores/transactions";

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

const apiMock = api as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

const tx1 = {
    id: "tx-1",
    type: "Income",
    amount: 1000,
    description: "Salary",
    date: "2026-02-15",
    categoryId: "cat-1",
    categoryName: "Housing",
    walletId: "w-1",
    destinationWalletId: null,
    createdAt: "2026-02-15T10:00:00Z",
};

const tx2 = {
    id: "tx-2",
    type: "Expense",
    amount: 50,
    description: "Coffee",
    date: "2026-02-16",
    categoryId: "cat-2",
    categoryName: "Food & Dining",
    walletId: "w-1",
    destinationWalletId: null,
    createdAt: "2026-02-16T09:00:00Z",
};

describe("useTransactionsStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("fetchTransactions populates the list and sets total", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { items: [tx1, tx2], total: 2 } });
        const store = useTransactionsStore();

        await store.fetchTransactions({ walletId: "w-1" });

        expect(store.transactions).toHaveLength(2);
        expect(store.total).toBe(2);
        expect(store.isLoading).toBe(false);
    });

    it("fetchTransactions passes walletId and filter params", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { items: [], total: 0 } });
        const store = useTransactionsStore();

        await store.fetchTransactions({ walletId: "w-1", from: "2026-01-01", type: "Income" });

        expect(apiMock.get).toHaveBeenCalledWith("/api/transactions", {
            params: expect.objectContaining({
                walletId: "w-1",
                from: "2026-01-01",
                type: "Income",
            }),
        });
    });

    it("incomeTransactions computed filters Income type", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { items: [tx1, tx2], total: 2 } });
        const store = useTransactionsStore();
        await store.fetchTransactions({ walletId: "w-1" });

        expect(store.incomeTransactions).toHaveLength(1);
        expect(store.incomeTransactions[0].id).toBe("tx-1");
    });

    it("expenseTransactions computed filters Expense type", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { items: [tx1, tx2], total: 2 } });
        const store = useTransactionsStore();
        await store.fetchTransactions({ walletId: "w-1" });

        expect(store.expenseTransactions).toHaveLength(1);
        expect(store.expenseTransactions[0].id).toBe("tx-2");
    });

    it("recordTransaction returns the new transaction without mutating the list", async () => {
        apiMock.post.mockResolvedValueOnce({ data: tx1 });
        const store = useTransactionsStore();

        const result = await store.recordTransaction({
            type: "Income",
            amount: 1000,
            description: "Salary",
            date: "2026-02-15",
            categoryId: "cat-1",
            walletId: "w-1",
        });

        expect(result).toEqual(tx1);
        // recordTransaction does NOT push to the list — the view re-fetches after recording
        expect(store.transactions).toHaveLength(0);
    });

    it("updateTransaction replaces the entry in the list", async () => {
        const updated = { ...tx1, amount: 1200, description: "Salary updated" };
        apiMock.get.mockResolvedValueOnce({ data: { items: [tx1], total: 1 } });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useTransactionsStore();
        await store.fetchTransactions({ walletId: "w-1" });

        await store.updateTransaction("tx-1", {
            amount: 1200,
            description: "Salary updated",
            date: "2026-02-15",
            categoryId: "cat-1",
        });

        expect(store.transactions[0].amount).toBe(1200);
        expect(store.transactions[0].description).toBe("Salary updated");
    });

    it("updateTransaction also refreshes currentTransaction when it matches", async () => {
        const updated = { ...tx1, amount: 1200 };
        apiMock.get.mockResolvedValueOnce({ data: tx1 });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useTransactionsStore();
        await store.fetchTransaction("tx-1");

        await store.updateTransaction("tx-1", {
            amount: 1200,
            description: "Salary",
            date: "2026-02-15",
            categoryId: "cat-1",
        });

        expect(store.currentTransaction?.amount).toBe(1200);
    });

    it("deleteTransaction removes the entry from the list and decrements total", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { items: [tx1, tx2], total: 2 } });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useTransactionsStore();
        await store.fetchTransactions({ walletId: "w-1" });

        await store.deleteTransaction("tx-1");

        expect(store.transactions).toHaveLength(1);
        expect(store.transactions[0].id).toBe("tx-2");
        expect(store.total).toBe(1);
    });

    it("deleteTransaction clears currentTransaction when it matches", async () => {
        apiMock.get.mockResolvedValueOnce({ data: tx1 });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useTransactionsStore();
        await store.fetchTransaction("tx-1");

        await store.deleteTransaction("tx-1");

        expect(store.currentTransaction).toBeNull();
    });
});
