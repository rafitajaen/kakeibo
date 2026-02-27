import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useGoalsStore } from "@/stores/goals";

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

const makeGoal = (overrides: Record<string, unknown> = {}) => ({
    id: "goal-1",
    name: "Europe Vacation",
    targetAmount: 5000,
    deadline: "2026-12-31",
    walletId: "wallet-1",
    walletName: "Checking",
    currentProgress: 1000,
    lastMilestone: 0,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

describe("useGoalsStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("fetchGoals populates the list", async () => {
        const goal = makeGoal();
        apiMock.get.mockResolvedValueOnce({ data: [goal] });
        const store = useGoalsStore();

        await store.fetchGoals();

        expect(store.goals).toHaveLength(1);
        expect(store.goals[0].name).toBe("Europe Vacation");
        expect(store.isLoading).toBe(false);
    });

    it("fetchGoals passes optional walletId param", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [] });
        const store = useGoalsStore();

        await store.fetchGoals("wallet-1");

        expect(apiMock.get).toHaveBeenCalledWith("/api/goals", {
            params: { walletId: "wallet-1" },
        });
    });

    it("fetchGoals omits walletId param when not provided", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [] });
        const store = useGoalsStore();

        await store.fetchGoals();

        expect(apiMock.get).toHaveBeenCalledWith("/api/goals", { params: {} });
    });

    it("achievedGoals computed returns goals where progress >= target", async () => {
        const active = makeGoal({ id: "g-1", currentProgress: 999, targetAmount: 5000 });
        const achieved = makeGoal({ id: "g-2", currentProgress: 5000, targetAmount: 5000 });
        apiMock.get.mockResolvedValueOnce({ data: [active, achieved] });
        const store = useGoalsStore();
        await store.fetchGoals();

        expect(store.achievedGoals).toHaveLength(1);
        expect(store.achievedGoals[0].id).toBe("g-2");
    });

    it("activeGoals computed returns goals where progress < target", async () => {
        const active = makeGoal({ id: "g-1", currentProgress: 999, targetAmount: 5000 });
        const achieved = makeGoal({ id: "g-2", currentProgress: 5000, targetAmount: 5000 });
        apiMock.get.mockResolvedValueOnce({ data: [active, achieved] });
        const store = useGoalsStore();
        await store.fetchGoals();

        expect(store.activeGoals).toHaveLength(1);
        expect(store.activeGoals[0].id).toBe("g-1");
    });

    it("createGoal prepends the new goal and returns it", async () => {
        const created = makeGoal({ id: "goal-new", name: "New Goal" });
        apiMock.post.mockResolvedValueOnce({ data: created });
        const store = useGoalsStore();

        const result = await store.createGoal({
            name: "New Goal",
            targetAmount: 5000,
            deadline: null,
            walletId: "wallet-1",
        });

        expect(result.id).toBe("goal-new");
        expect(store.goals).toHaveLength(1);
        expect(store.goals[0].name).toBe("New Goal");
    });

    it("createGoal places the new goal at the front of the list", async () => {
        const existing = makeGoal({ id: "goal-old", name: "Old Goal" });
        const created = makeGoal({ id: "goal-new", name: "New Goal" });
        apiMock.get.mockResolvedValueOnce({ data: [existing] });
        apiMock.post.mockResolvedValueOnce({ data: created });
        const store = useGoalsStore();
        await store.fetchGoals();

        await store.createGoal({ name: "New Goal", targetAmount: 5000, walletId: "wallet-1" });

        expect(store.goals[0].id).toBe("goal-new");
        expect(store.goals[1].id).toBe("goal-old");
    });

    it("updateGoal replaces the entry in the list", async () => {
        const original = makeGoal();
        const updated = makeGoal({ name: "Updated Goal", targetAmount: 8000 });
        apiMock.get.mockResolvedValueOnce({ data: [original] });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useGoalsStore();
        await store.fetchGoals();

        await store.updateGoal("goal-1", {
            name: "Updated Goal",
            targetAmount: 8000,
            deadline: null,
            walletId: "wallet-1",
        });

        expect(store.goals[0].name).toBe("Updated Goal");
        expect(store.goals[0].targetAmount).toBe(8000);
    });

    it("deleteGoal removes the entry from the list", async () => {
        const goal = makeGoal();
        apiMock.get.mockResolvedValueOnce({ data: [goal] });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useGoalsStore();
        await store.fetchGoals();

        await store.deleteGoal("goal-1");

        expect(store.goals).toHaveLength(0);
        expect(apiMock.delete).toHaveBeenCalledWith("/api/goals/goal-1");
    });

    it("getGoalProgress calls the progress endpoint and returns the response", async () => {
        const progress = {
            id: "goal-1",
            name: "Europe Vacation",
            targetAmount: 5000,
            currentProgress: 1000,
            remaining: 4000,
            percentageComplete: 20,
            status: "InProgress",
            deadline: "2026-12-31",
            daysRemaining: 310,
        };
        apiMock.get.mockResolvedValueOnce({ data: progress });
        const store = useGoalsStore();

        const result = await store.getGoalProgress("goal-1");

        expect(result.status).toBe("InProgress");
        expect(result.percentageComplete).toBe(20);
        expect(apiMock.get).toHaveBeenCalledWith("/api/goals/goal-1/progress");
    });
});
