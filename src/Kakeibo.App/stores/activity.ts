import { ref } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface ActivityEntry {
    id: string;
    action: string;
    entityType: string | null;
    entityId: string | null;
    occurredAt: string;
}

export interface ActivityFilters {
    start?: string;
    end?: string;
    type?: string;
    page: number;
    pageSize: number;
}

interface ActivityFeedResponse {
    items: ActivityEntry[];
    total: number;
    page: number;
    pageSize: number;
}

export const useActivityStore = defineStore("activity", () => {
    // State
    const activities = ref<ActivityEntry[]>([]);
    const total = ref(0);
    const isLoading = ref(false);
    const filters = ref<ActivityFilters>({ page: 1, pageSize: 20 });

    // Fetches the activity feed from the API, applying current filters.
    async function fetchActivities(newFilters?: Partial<ActivityFilters>): Promise<void> {
        if (newFilters) {
            filters.value = { ...filters.value, ...newFilters, page: newFilters.page ?? 1 };
        }

        isLoading.value = true;
        try {
            const params: Record<string, string | number> = {
                page: filters.value.page,
                pageSize: filters.value.pageSize,
            };
            if (filters.value.start) params.start = filters.value.start;
            if (filters.value.end) params.end = filters.value.end;
            if (filters.value.type) params.type = filters.value.type;

            const response = await api.get<ActivityFeedResponse>("/api/activity", { params });
            activities.value = response.data.items;
            total.value = response.data.total;
            filters.value.page = response.data.page;
        } finally {
            isLoading.value = false;
        }
    }

    return {
        activities,
        total,
        isLoading,
        filters,
        fetchActivities,
    };
});
