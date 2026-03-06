import { ref } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Session {
    id: string;
    ipAddress: string | null;
    userAgent: string | null;
    createdAt: string;
}

export interface UpdateProfileData {
    name?: string | null;
    currency?: string;
}

export interface ChangePasswordData {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
}

export interface UpdateDisplayPreferencesData {
    weekStartDay?: number | null;
    monthStartDay?: number | null;
    currencyDecimalSeparator?: string | null;
    currencyGroupSeparator?: string | null;
    currencySymbolPosition?: string | null;
    currencyDisplay?: string | null;
}

export const useSettingsStore = defineStore("settings", () => {
    const sessions = ref<Session[]>([]);
    const isLoading = ref(false);

    // Fetches the list of active sessions for the current user.
    async function fetchSessions(): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<{ sessions: Session[] }>("/api/users/me/sessions");
            sessions.value = response.data.sessions;
        } finally {
            isLoading.value = false;
        }
    }

    // Updates the display name and/or currency of the current user.
    async function updateProfile(data: UpdateProfileData): Promise<void> {
        await api.put("/api/users/me/profile", data);
    }

    // Changes the password after verifying the current one.
    async function changePassword(data: ChangePasswordData): Promise<void> {
        await api.put("/api/users/me/password", data);
    }

    // Initiates account deletion (30-day grace period before permanent deletion).
    async function deleteAccount(password: string): Promise<void> {
        await api.delete("/api/users/me", { data: { password } });
    }

    // Revokes a specific session by ID.
    async function revokeSession(sessionId: string): Promise<void> {
        await api.delete(`/api/users/me/sessions/${sessionId}`);
        sessions.value = sessions.value.filter((s) => s.id !== sessionId);
    }

    // Updates the user's display preferences (week/month start, currency format).
    async function updateDisplayPreferences(data: UpdateDisplayPreferencesData): Promise<void> {
        await api.put("/api/users/me/display-preferences", data);
    }

    return {
        sessions,
        isLoading,
        fetchSessions,
        updateProfile,
        changePassword,
        deleteAccount,
        revokeSession,
        updateDisplayPreferences,
    };
});
