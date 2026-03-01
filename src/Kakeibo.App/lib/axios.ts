import axios from "axios";
import { useAuthStore } from "@/stores/auth";

// Axios instance with HttpOnly-cookie-based auth.
// Tokens travel automatically via cookies — no Authorization header needed.
const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL ?? "",
    withCredentials: true,
});

// Attach X-User-Id on every request so backend [FromHeader] parameters receive it.
// useAuthStore() is called inside the interceptor callback (not at module init time)
// so Pinia is already initialized by the time the first request fires.
api.interceptors.request.use((config) => {
    const auth = useAuthStore();
    if (auth.user?.id) {
        config.headers["X-User-Id"] = auth.user.id;
    }
    return config;
});

// Tracks whether a token refresh is currently in flight to avoid
// concurrent calls and queue subsequent 401 requests.
let isRefreshing = false;
let pendingRequests: Array<() => void> = [];

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        // Never attempt refresh for auth-check or refresh endpoints — let callers handle 401s directly.
        // Without this guard, GET /api/auth/me (called on every page load to restore session)
        // would trigger a refresh attempt and redirect to /login even on public routes like /register.
        const skipRefreshUrls = ["/api/auth/me", "/api/auth/refresh"];
        if (skipRefreshUrls.some((url) => originalRequest.url?.includes(url))) {
            return Promise.reject(error);
        }

        // Only attempt refresh once per request (_retry flag) and only on 401.
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            if (!isRefreshing) {
                isRefreshing = true;

                try {
                    // Refresh the access token using the HttpOnly refresh_token cookie.
                    await api.post("/api/auth/refresh");

                    // Flush all queued requests that arrived while refreshing.
                    pendingRequests.forEach((resolve) => resolve());
                    pendingRequests = [];

                    return api(originalRequest);
                } catch {
                    pendingRequests = [];
                    // Redirect to login when the refresh token is also expired/revoked.
                    window.location.href = "/login";
                    return Promise.reject(error);
                } finally {
                    isRefreshing = false;
                }
            }

            // Queue the request while a refresh is already in progress.
            return new Promise((resolve) => {
                pendingRequests.push(() => resolve(api(originalRequest)));
            });
        }

        return Promise.reject(error);
    },
);

export default api;
