import axios from "axios";

// Axios instance with HttpOnly-cookie-based auth.
// Tokens travel automatically via cookies — no Authorization header needed.
const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5000",
    withCredentials: true,
});

// Tracks whether a token refresh is currently in flight to avoid
// concurrent calls and queue subsequent 401 requests.
let isRefreshing = false;
let pendingRequests: Array<() => void> = [];

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

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
