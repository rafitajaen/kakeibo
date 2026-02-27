import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useBudgetsStore } from "@/stores/budgets";

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

const makeBudget = (overrides: Record<string, unknown> = {}) => ({
    id: "budget-1",
    name: "Food Budget",
    categoryId: "cat-1",
    categoryName: "Food & Dining",
    limit: 400,
    startDate: "2026-01-01",
    endDate: "2026-01-31",
    walletId: null,
    walletName: null,
    currentSpending: 0,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

describe("useBudgetsStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("fetchBudgets populates the list", async () => {
        const budget = makeBudget();
        apiMock.get.mockResolvedValueOnce({ data: [budget] });
        const store = useBudgetsStore();

        await store.fetchBudgets();

        expect(store.budgets).toHaveLength(1);
        expect(store.budgets[0].name).toBe("Food Budget");
        expect(store.isLoading).toBe(false);
    });

    it("fetchBudgets passes optional query params", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [] });
        const store = useBudgetsStore();

        await store.fetchBudgets("cat-1", "wallet-1");

        expect(apiMock.get).toHaveBeenCalledWith("/api/budgets", {
            params: { categoryId: "cat-1", walletId: "wallet-1" },
        });
    });

    it("onTrackBudgets computed returns budgets below 75% threshold", async () => {
        const low = makeBudget({ id: "b-1", currentSpending: 74, limit: 100 });
        const warn = makeBudget({ id: "b-2", currentSpending: 75, limit: 100 });
        apiMock.get.mockResolvedValueOnce({ data: [low, warn] });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        expect(store.onTrackBudgets).toHaveLength(1);
        expect(store.onTrackBudgets[0].id).toBe("b-1");
    });

    it("warningBudgets computed returns budgets between 75% and 100%", async () => {
        const warn = makeBudget({ id: "b-2", currentSpending: 75, limit: 100 });
        const exceeded = makeBudget({ id: "b-3", currentSpending: 100, limit: 100 });
        apiMock.get.mockResolvedValueOnce({ data: [warn, exceeded] });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        expect(store.warningBudgets).toHaveLength(1);
        expect(store.warningBudgets[0].id).toBe("b-2");
    });

    it("exceededBudgets computed returns budgets at or above 100%", async () => {
        const budget = makeBudget({ currentSpending: 100, limit: 100 });
        apiMock.get.mockResolvedValueOnce({ data: [budget] });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        expect(store.exceededBudgets).toHaveLength(1);
    });

    it("createBudget appends the new budget and returns it", async () => {
        const created = makeBudget({ id: "budget-new", name: "New Budget" });
        apiMock.post.mockResolvedValueOnce({ data: created });
        const store = useBudgetsStore();

        const result = await store.createBudget({
            name: "New Budget",
            categoryId: "cat-1",
            limit: 400,
            startDate: "2026-01-01",
            endDate: "2026-01-31",
        });

        expect(result.id).toBe("budget-new");
        expect(store.budgets).toHaveLength(1);
        expect(store.budgets[0].name).toBe("New Budget");
    });

    it("updateBudget replaces the entry in the list", async () => {
        const original = makeBudget();
        const updated = makeBudget({ name: "Updated Budget", limit: 500 });
        apiMock.get.mockResolvedValueOnce({ data: [original] });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useBudgetsStore();
        await store.fetchBudgets();

        await store.updateBudget("budget-1", {
            name: "Updated Budget",
            categoryId: "cat-1",
            limit: 500,
            startDate: "2026-01-01",
            endDate: "2026-01-31",
        });

        expect(store.budgets[0].name).toBe("Updated Budget");
        expect(store.budgets[0].limit).toBe(500);
    });

    it("deleteBudget removes the entry from the list", async () => {
        const budget = makeBudget();
        apiMock.get.mockResolvedValueOnce({ data: [budget] });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useBudgetsStore();
        await store.fetchBudgets();

        await store.deleteBudget("budget-1");

        expect(store.budgets).toHaveLength(0);
        expect(apiMock.delete).toHaveBeenCalledWith("/api/budgets/budget-1");
    });

    it("getBudgetStatus calls the status endpoint and returns the response", async () => {
        const status = {
            id: "budget-1",
            name: "Food Budget",
            categoryId: "cat-1",
            categoryName: "Food & Dining",
            limit: 400,
            currentSpending: 100,
            remaining: 300,
            percentageUsed: 25,
            status: "OnTrack",
            startDate: "2026-01-01",
            endDate: "2026-01-31",
            walletId: null,
            walletName: null,
        };
        apiMock.get.mockResolvedValueOnce({ data: status });
        const store = useBudgetsStore();

        const result = await store.getBudgetStatus("budget-1");

        expect(result.status).toBe("OnTrack");
        expect(result.percentageUsed).toBe(25);
        expect(apiMock.get).toHaveBeenCalledWith("/api/budgets/budget-1/status");
    });
});
