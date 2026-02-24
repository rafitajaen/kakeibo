import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Category {
    id: string;
    name: string;
    isSystem: boolean;
    isArchived: boolean;
}

export interface CreateCategoryData {
    name: string;
}

export interface UpdateCategoryData {
    name: string;
}

export const useCategoriesStore = defineStore("categories", () => {
    // State
    const categories = ref<Category[]>([]);
    const isLoading = ref(false);

    // Getters
    const systemCategories = computed(() => categories.value.filter((c) => c.isSystem));
    const customCategories = computed(() => categories.value.filter((c) => !c.isSystem));
    const activeCategories = computed(() => categories.value.filter((c) => !c.isArchived));

    // Fetches all categories. Pass includeArchived=true to include archived custom categories.
    async function fetchCategories(includeArchived = false): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<Category[]>("/api/categories", {
                params: { includeArchived },
            });
            categories.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Creates a new custom category and appends it to the local list.
    async function createCategory(data: CreateCategoryData): Promise<Category> {
        const response = await api.post<Category>("/api/categories", data);
        categories.value.push(response.data);
        return response.data;
    }

    // Renames a custom category and refreshes the local list entry.
    async function updateCategory(id: string, data: UpdateCategoryData): Promise<Category> {
        const response = await api.put<Category>(`/api/categories/${id}`, data);
        const index = categories.value.findIndex((c) => c.id === id);
        if (index !== -1) {
            categories.value[index] = response.data;
        }
        return response.data;
    }

    // Archives (soft-deletes) a custom category and marks it as archived in the local list.
    async function archiveCategory(id: string): Promise<void> {
        await api.delete(`/api/categories/${id}`);
        const index = categories.value.findIndex((c) => c.id === id);
        if (index !== -1) {
            categories.value[index] = { ...categories.value[index], isArchived: true };
        }
    }

    return {
        categories,
        isLoading,
        systemCategories,
        customCategories,
        activeCategories,
        fetchCategories,
        createCategory,
        updateCategory,
        archiveCategory,
    };
});
