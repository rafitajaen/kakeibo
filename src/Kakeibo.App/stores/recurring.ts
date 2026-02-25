import { ref } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface RecurringPattern {
    id: string;
    name: string;
    amount: number;
    description: string;
    transactionType: string;
    frequency: string;
    categoryId: string;
    categoryName: string;
    walletId: string;
    walletName: string;
    destinationWalletId: string | null;
    destinationWalletName: string | null;
    startDate: string;
    endDate: string | null;
    nextOccurrence: string;
    isActive: boolean;
    createdAt: string;
}

export interface CreatePatternData {
    name: string;
    amount: number;
    description: string;
    transactionType: string;
    frequency: string;
    categoryId: string;
    walletId: string;
    destinationWalletId?: string | null;
    startDate: string;
    endDate?: string | null;
}

export interface UpdatePatternData {
    name: string;
    amount: number;
    description: string;
    transactionType: string;
    frequency: string;
    categoryId: string;
    walletId: string;
    destinationWalletId?: string | null;
    endDate?: string | null;
}

export interface ForecastItem {
    patternId: string;
    patternName: string;
    transactionType: string;
    amount: number;
    categoryName: string;
    walletName: string;
    date: string;
    frequency: string;
}

export const useRecurringStore = defineStore("recurring", () => {
    // State
    const patterns = ref<RecurringPattern[]>([]);
    const forecast = ref<ForecastItem[]>([]);
    const isLoading = ref(false);

    // Fetches all recurring patterns for the current user.
    async function fetchPatterns(): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<RecurringPattern[]>("/api/recurring-patterns");
            patterns.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Creates a new recurring pattern and prepends it to the local list.
    async function createPattern(data: CreatePatternData): Promise<RecurringPattern> {
        const response = await api.post<RecurringPattern>("/api/recurring-patterns", data);
        patterns.value.unshift(response.data);
        return response.data;
    }

    // Updates a pattern and refreshes the local list entry.
    async function updatePattern(id: string, data: UpdatePatternData): Promise<RecurringPattern> {
        const response = await api.put<RecurringPattern>(`/api/recurring-patterns/${id}`, data);
        const index = patterns.value.findIndex((p) => p.id === id);
        if (index !== -1) {
            patterns.value[index] = response.data;
        }
        return response.data;
    }

    // Soft-deletes a pattern and removes it from the local list.
    async function deletePattern(id: string): Promise<void> {
        await api.delete(`/api/recurring-patterns/${id}`);
        patterns.value = patterns.value.filter((p) => p.id !== id);
    }

    // Fetches the forecast for the next N days (default 30, max 90).
    async function fetchForecast(days = 30): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<ForecastItem[]>("/api/recurring-patterns/forecast", {
                params: { days },
            });
            forecast.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    return {
        patterns,
        forecast,
        isLoading,
        fetchPatterns,
        createPattern,
        updatePattern,
        deletePattern,
        fetchForecast,
    };
});
