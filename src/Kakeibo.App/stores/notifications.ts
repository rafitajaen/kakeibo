import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface AppNotification {
    id: string;
    type: string;
    title: string;
    body: string;
    isRead: boolean;
    createdAt: string;
}

export interface NotificationPreferences {
    emailBudgetAlerts: boolean;
    emailGoalMilestones: boolean;
    emailInvitations: boolean;
    emailRecurringUpdates: boolean;
    pushBudgetAlerts: boolean;
    pushGoalMilestones: boolean;
    pushInvitations: boolean;
    pushRecurringUpdates: boolean;
}

interface ListNotificationsResponse {
    unreadCount: number;
    items: AppNotification[];
}

export const useNotificationsStore = defineStore("notifications", () => {
    // State
    const notifications = ref<AppNotification[]>([]);
    const unreadCount = computed(() => notifications.value.filter((n) => !n.isRead).length);
    const preferences = ref<NotificationPreferences | null>(null);
    const isLoading = ref(false);

    // Fetches the latest page of notifications from the API.
    async function fetchNotifications(): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<ListNotificationsResponse>("/api/notifications", {
                params: { page: 1, pageSize: 20 },
            });
            notifications.value = response.data.items;
        } finally {
            isLoading.value = false;
        }
    }

    // Marks a single notification as read locally and on the server.
    async function markAsRead(id: string): Promise<void> {
        await api.put(`/api/notifications/${id}/read`);
        const notification = notifications.value.find((n) => n.id === id);
        if (notification) {
            notification.isRead = true;
        }
    }

    // Marks all currently loaded notifications as read.
    async function markAllAsRead(): Promise<void> {
        const unread = notifications.value.filter((n) => !n.isRead);
        await Promise.all(unread.map((n) => markAsRead(n.id)));
    }

    // Fetches notification channel preferences for the current user.
    async function fetchPreferences(): Promise<void> {
        const response = await api.get<NotificationPreferences>("/api/notifications/preferences");
        preferences.value = response.data;
    }

    // Updates notification channel preferences for the current user.
    async function updatePreferences(prefs: NotificationPreferences): Promise<void> {
        await api.put("/api/notifications/preferences", prefs);
        preferences.value = prefs;
    }

    // Registers a Web Push subscription endpoint with the server.
    async function registerPushSubscription(subscription: PushSubscription): Promise<void> {
        const raw = subscription.toJSON();
        await api.post("/api/users/me/push-subscriptions", {
            endpoint: subscription.endpoint,
            p256dh: raw.keys?.p256dh ?? "",
            auth: raw.keys?.auth ?? "",
        });
    }

    return {
        notifications,
        unreadCount,
        preferences,
        isLoading,
        fetchNotifications,
        markAsRead,
        markAllAsRead,
        fetchPreferences,
        updatePreferences,
        registerPushSubscription,
    };
});
