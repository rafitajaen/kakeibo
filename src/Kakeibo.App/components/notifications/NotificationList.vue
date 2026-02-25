<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { formatDistanceToNow, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import { useNotificationsStore } from "@/stores/notifications";

const emit = defineEmits<{
    (e: "close"): void;
}>();

const { t } = useI18n();
const router = useRouter();
const store = useNotificationsStore();

// Returns a human-readable relative time (e.g. "2 hours ago").
function relativeTime(isoString: string): string {
    try {
        return formatDistanceToNow(parseISO(isoString), { addSuffix: true });
    } catch {
        return isoString;
    }
}

async function handleMarkAllRead() {
    await store.markAllAsRead();
}

async function handleMarkRead(id: string) {
    await store.markAsRead(id);
}

function goToPreferences() {
    router.push({ name: "notification-preferences" });
    emit("close");
}
</script>

<template>
    <div
        class="absolute right-0 top-full mt-2 w-80 bg-background border rounded-lg shadow-lg z-50 overflow-hidden"
        data-testid="notification-list"
    >
        <!-- Header -->
        <div class="flex items-center justify-between p-3 border-b">
            <span class="font-semibold text-sm">{{ t("notifications.title") }}</span>
            <div class="flex items-center gap-2">
                <button
                    v-if="store.unreadCount > 0"
                    class="text-xs text-muted-foreground hover:text-foreground"
                    @click="handleMarkAllRead"
                >
                    {{ t("notifications.markAllRead") }}
                </button>
                <button
                    class="text-muted-foreground hover:text-foreground"
                    :aria-label="t('notifications.close')"
                    @click="emit('close')"
                >
                    ✕
                </button>
            </div>
        </div>

        <!-- Notification items -->
        <div class="max-h-96 overflow-y-auto">
            <div
                v-if="store.notifications.length === 0"
                class="p-4 text-center text-muted-foreground text-sm"
            >
                {{ t("notifications.noNotifications") }}
            </div>

            <div
                v-for="notification in store.notifications"
                :key="notification.id"
                :class="[
                    'p-3 border-b last:border-b-0 cursor-pointer hover:bg-muted/50 transition-colors',
                    !notification.isRead && 'bg-muted/30',
                ]"
                data-testid="notification-item"
                @click="handleMarkRead(notification.id)"
            >
                <div class="flex items-start gap-2">
                    <!-- Unread dot -->
                    <div
                        v-if="!notification.isRead"
                        class="mt-1.5 h-2 w-2 rounded-full bg-primary flex-shrink-0"
                    />
                    <div :class="!notification.isRead ? '' : 'ml-4'">
                        <p class="text-sm font-medium">{{ notification.title }}</p>
                        <p class="text-xs text-muted-foreground mt-0.5">{{ notification.body }}</p>
                        <p class="text-xs text-muted-foreground mt-1">
                            {{ relativeTime(notification.createdAt) }}
                        </p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Footer link to preferences -->
        <div class="p-2 border-t">
            <Button variant="ghost" size="sm" class="w-full text-xs" @click="goToPreferences">
                {{ t("notifications.preferences") }}
            </Button>
        </div>
    </div>
</template>
