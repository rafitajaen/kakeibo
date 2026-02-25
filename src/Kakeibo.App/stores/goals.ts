import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Goal {
    id: string;
    name: string;
    targetAmount: number;
    deadline: string | null;
    walletId: string;
    walletName: string;
    currentProgress: number;
    lastMilestone: number;
    createdAt: string;
}

export interface CreateGoalData {
    name: string;
    targetAmount: number;
    deadline?: string | null;
    walletId: string;
}

export interface UpdateGoalData {
    name: string;
    targetAmount: number;
    deadline?: string | null;
    walletId: string;
}

export interface GoalProgress {
    id: string;
    name: string;
    targetAmount: number;
    currentProgress: number;
    remaining: number;
    percentageComplete: number;
    status: "NotStarted" | "InProgress" | "NearTarget" | "Achieved";
    deadline: string | null;
    daysRemaining: number | null;
}

export const useGoalsStore = defineStore("goals", () => {
    // State
    const goals = ref<Goal[]>([]);
    const isLoading = ref(false);

    // Getters — derived from currentProgress vs targetAmount
    const achievedGoals = computed(() =>
        goals.value.filter((g) => g.currentProgress >= g.targetAmount),
    );
    const activeGoals = computed(() =>
        goals.value.filter((g) => g.currentProgress < g.targetAmount),
    );

    // Fetches all goals for the current user with optional wallet filter.
    async function fetchGoals(walletId?: string): Promise<void> {
        isLoading.value = true;
        try {
            const params: Record<string, string> = {};
            if (walletId) params.walletId = walletId;
            const response = await api.get<Goal[]>("/api/goals", { params });
            goals.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Creates a new goal and prepends it to the local list.
    async function createGoal(data: CreateGoalData): Promise<Goal> {
        const response = await api.post<Goal>("/api/goals", data);
        goals.value.unshift(response.data);
        return response.data;
    }

    // Updates an existing goal and refreshes the local list entry.
    async function updateGoal(id: string, data: UpdateGoalData): Promise<Goal> {
        const response = await api.put<Goal>(`/api/goals/${id}`, data);
        const index = goals.value.findIndex((g) => g.id === id);
        if (index !== -1) {
            goals.value[index] = response.data;
        }
        return response.data;
    }

    // Soft-deletes a goal and removes it from the local list.
    async function deleteGoal(id: string): Promise<void> {
        await api.delete(`/api/goals/${id}`);
        goals.value = goals.value.filter((g) => g.id !== id);
    }

    // Fetches computed progress (remaining, percentage, status, daysRemaining) for one goal.
    async function getGoalProgress(id: string): Promise<GoalProgress> {
        const response = await api.get<GoalProgress>(`/api/goals/${id}/progress`);
        return response.data;
    }

    return {
        goals,
        isLoading,
        achievedGoals,
        activeGoals,
        fetchGoals,
        createGoal,
        updateGoal,
        deleteGoal,
        getGoalProgress,
    };
});
