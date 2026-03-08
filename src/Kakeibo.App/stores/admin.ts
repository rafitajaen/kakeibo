import { ref } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface PlatformPolicy {
    registrationEnabled: boolean;
    maintenanceMode: boolean;
}

export interface AdminUser {
    id: string;
    email: string;
    username: string;
    name: string | null;
    role: string;
    isVerified: boolean;
    isBlocked: boolean;
    hasPassword: boolean;
    hasGoogleId: boolean;
    createdAt: string;
}

export interface CreateUserPayload {
    email: string;
    password: string;
    currency: string;
    name?: string;
    role: string;
}

export interface UpdateUserPayload {
    name?: string;
    role?: string;
}

export const useAdminStore = defineStore("admin", () => {
    // State
    const policy = ref<PlatformPolicy | null>(null);
    const users = ref<AdminUser[]>([]);
    const loadingPolicy = ref(false);
    const loadingUsers = ref(false);
    const error = ref<string | null>(null);

    // Actions
    async function fetchPolicy() {
        loadingPolicy.value = true;
        error.value = null;
        try {
            const { data } = await api.get<PlatformPolicy>("/api/admin/settings");
            policy.value = data;
        } catch (e: unknown) {
            error.value = e instanceof Error ? e.message : "Failed to load platform settings";
        } finally {
            loadingPolicy.value = false;
        }
    }

    async function updatePolicy(payload: PlatformPolicy) {
        loadingPolicy.value = true;
        error.value = null;
        try {
            const { data } = await api.put<PlatformPolicy>("/api/admin/settings", payload);
            policy.value = data;
        } catch (e: unknown) {
            error.value = e instanceof Error ? e.message : "Failed to update platform settings";
            throw e;
        } finally {
            loadingPolicy.value = false;
        }
    }

    async function fetchUsers(page = 1, pageSize = 50, search?: string) {
        loadingUsers.value = true;
        error.value = null;
        try {
            const params: Record<string, unknown> = { page, pageSize };
            if (search) params.search = search;
            const { data } = await api.get<AdminUser[]>("/api/admin/users", { params });
            users.value = data;
        } catch (e: unknown) {
            error.value = e instanceof Error ? e.message : "Failed to load users";
        } finally {
            loadingUsers.value = false;
        }
    }

    async function createUser(payload: CreateUserPayload) {
        const { data } = await api.post<AdminUser>("/api/admin/users", payload);
        users.value.unshift({
            ...data,
            isVerified: true,
            isBlocked: false,
            hasGoogleId: false,
            createdAt: new Date().toISOString(),
        });
        return data;
    }

    async function updateUser(userId: string, payload: UpdateUserPayload) {
        const { data } = await api.put<AdminUser>(`/api/admin/users/${userId}`, payload);
        const idx = users.value.findIndex((u) => u.id === userId);
        if (idx !== -1) users.value[idx] = { ...users.value[idx], ...data };
        return data;
    }

    async function blockUser(userId: string) {
        await api.post(`/api/admin/users/${userId}/block`);
        const user = users.value.find((u) => u.id === userId);
        if (user) user.isBlocked = true;
    }

    async function unblockUser(userId: string) {
        await api.post(`/api/admin/users/${userId}/unblock`);
        const user = users.value.find((u) => u.id === userId);
        if (user) user.isBlocked = false;
    }

    async function deleteUser(userId: string) {
        await api.delete(`/api/admin/users/${userId}`);
        users.value = users.value.filter((u) => u.id !== userId);
    }

    return {
        policy,
        users,
        loadingPolicy,
        loadingUsers,
        error,
        fetchPolicy,
        updatePolicy,
        fetchUsers,
        createUser,
        updateUser,
        blockUser,
        unblockUser,
        deleteUser,
    };
});
