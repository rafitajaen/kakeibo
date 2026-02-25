import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Budget {
    id: string;
    name: string;
    categoryId: string;
    categoryName: string;
    limit: number;
    startDate: string;
    endDate: string;
    walletId: string | null;
    walletName: string | null;
    currentSpending: number;
    createdAt: string;
}

export interface CreateBudgetData {
    name: string;
    categoryId: string;
    limit: number;
    startDate: string;
    endDate: string;
    walletId?: string | null;
}

export interface UpdateBudgetData {
    name: string;
    categoryId: string;
    limit: number;
    startDate: string;
    endDate: string;
    walletId?: string | null;
}

export interface BudgetStatus {
    id: string;
    name: string;
    categoryId: string;
    categoryName: string;
    limit: number;
    currentSpending: number;
    remaining: number;
    percentageUsed: number;
    status: "OnTrack" | "Warning" | "Exceeded";
    startDate: string;
    endDate: string;
    walletId: string | null;
    walletName: string | null;
}

export const useBudgetsStore = defineStore("budgets", () => {
    // State
    const budgets = ref<Budget[]>([]);
    const isLoading = ref(false);

    // Getters — derived from currentSpending vs limit thresholds
    const onTrackBudgets = computed(() =>
        budgets.value.filter((b) => b.currentSpending < b.limit * 0.75),
    );
    const warningBudgets = computed(() =>
        budgets.value.filter(
            (b) => b.currentSpending >= b.limit * 0.75 && b.currentSpending < b.limit,
        ),
    );
    const exceededBudgets = computed(() =>
        budgets.value.filter((b) => b.currentSpending >= b.limit),
    );

    // Fetches all budgets for the current user with optional filters.
    async function fetchBudgets(categoryId?: string, walletId?: string): Promise<void> {
        isLoading.value = true;
        try {
            const params: Record<string, string> = {};
            if (categoryId) params.categoryId = categoryId;
            if (walletId) params.walletId = walletId;
            const response = await api.get<Budget[]>("/api/budgets", { params });
            budgets.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Creates a new budget and appends it to the local list.
    async function createBudget(data: CreateBudgetData): Promise<Budget> {
        const response = await api.post<Budget>("/api/budgets", data);
        budgets.value.unshift(response.data);
        return response.data;
    }

    // Updates an existing budget and refreshes the local list entry.
    async function updateBudget(id: string, data: UpdateBudgetData): Promise<Budget> {
        const response = await api.put<Budget>(`/api/budgets/${id}`, data);
        const index = budgets.value.findIndex((b) => b.id === id);
        if (index !== -1) {
            budgets.value[index] = response.data;
        }
        return response.data;
    }

    // Soft-deletes a budget and removes it from the local list.
    async function deleteBudget(id: string): Promise<void> {
        await api.delete(`/api/budgets/${id}`);
        budgets.value = budgets.value.filter((b) => b.id !== id);
    }

    // Fetches computed status (spending, remaining, percentage, status string) for one budget.
    async function getBudgetStatus(id: string): Promise<BudgetStatus> {
        const response = await api.get<BudgetStatus>(`/api/budgets/${id}/status`);
        return response.data;
    }

    return {
        budgets,
        isLoading,
        onTrackBudgets,
        warningBudgets,
        exceededBudgets,
        fetchBudgets,
        createBudget,
        updateBudget,
        deleteBudget,
        getBudgetStatus,
    };
});
