import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface User {
    id: string;
    email: string;
    username: string;
    role: string;
    currency: string;
    isVerified: boolean;
    hasPassword: boolean;
    name: string | null;
    avatarUrl: string | null;
    // Display preferences
    weekStartDay: number;
    monthStartDay: number;
    currencyDecimalSeparator: string;
    currencyGroupSeparator: string;
    currencySymbolPosition: string;
    currencyDisplay: string;
}

export interface RegisterData {
    email: string;
    password: string;
    currency: string;
}

export interface LoginData {
    email: string;
    password: string;
}

export const useAuthStore = defineStore("auth", () => {
    // State
    const user = ref<User | null>(null);
    // True after the first fetchCurrentUser() call completes (success or failure).
    const initialized = ref(false);

    // Getters
    const isAuthenticated = computed(() => user.value !== null);

    // Clears local auth state without calling the API (used on forced logout).
    function clearUser() {
        user.value = null;
    }

    // Registers a new user account. Does not log in automatically.
    async function register(data: RegisterData): Promise<void> {
        await api.post("/api/auth/register", data);
    }

    // Verifies the user's email using the token sent in the verification email.
    async function verifyEmail(token: string): Promise<void> {
        await api.post("/api/auth/verify-email", { token });
    }

    // Authenticates the user. Sets access_token and refresh_token HttpOnly cookies.
    async function login(data: LoginData): Promise<void> {
        await api.post("/api/auth/login", data);
        await fetchCurrentUser();
    }

    // Authenticates or registers via Google ID token.
    async function loginWithGoogle(idToken: string, currency = "EUR"): Promise<void> {
        await api.post("/api/auth/google", { idToken, currency });
        await fetchCurrentUser();
    }

    // Updates the authenticated user's username.
    async function updateUsername(username: string): Promise<string> {
        const response = await api.put<{ username: string }>("/api/users/me/username", {
            username,
        });
        if (user.value) {
            user.value.username = response.data.username;
        }
        return response.data.username;
    }

    // Rotates the access token using the refresh_token cookie.
    async function refresh(): Promise<void> {
        await api.post("/api/auth/refresh");
    }

    // Revokes the current session's refresh token and clears auth state.
    async function logout(): Promise<void> {
        try {
            await api.post("/api/auth/logout");
        } finally {
            user.value = null;
        }
    }

    // Sends a password-reset email with a secure token link.
    async function forgotPassword(email: string): Promise<void> {
        await api.post("/api/auth/forgot-password", { email });
    }

    // Resets the password using the token from the recovery email.
    async function resetPassword(
        token: string,
        newPassword: string,
        confirmPassword: string,
    ): Promise<void> {
        await api.post("/api/auth/reset-password", {
            token,
            newPassword,
            confirmPassword,
        });
    }

    // Fetches the current user's profile on app init.
    // The catch is intentional: a 401 here is expected when the user is not logged in.
    // The axios interceptor skips token refresh for /api/auth/me, so the error is silenced.
    async function fetchCurrentUser(): Promise<void> {
        try {
            const response = await api.get<User>("/api/auth/me");
            user.value = response.data;
        } catch {
            user.value = null;
        } finally {
            initialized.value = true;
        }
    }

    return {
        user,
        initialized,
        isAuthenticated,
        clearUser,
        register,
        verifyEmail,
        login,
        loginWithGoogle,
        updateUsername,
        refresh,
        logout,
        forgotPassword,
        resetPassword,
        fetchCurrentUser,
    };
});
