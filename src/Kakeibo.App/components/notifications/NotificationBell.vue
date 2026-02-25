<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useNotificationsStore } from "@/stores/notifications";
import NotificationList from "@/components/notifications/NotificationList.vue";

const { t } = useI18n();
const store = useNotificationsStore();
const isOpen = ref(false);

onMounted(async () => {
    await store.fetchNotifications();
});

function toggle() {
    isOpen.value = !isOpen.value;
}
</script>

<template>
    <div class="relative">
        <button
            class="relative p-2 rounded-full hover:bg-muted transition-colors"
            :aria-label="t('notifications.title')"
            data-testid="notification-bell"
            @click="toggle"
        >
            <svg
                xmlns="http://www.w3.org/2000/svg"
                class="h-6 w-6"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
            >
                <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                />
            </svg>

            <!-- Unread badge -->
            <span
                v-if="store.unreadCount > 0"
                class="absolute -top-1 -right-1 bg-destructive text-destructive-foreground text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center"
                data-testid="unread-badge"
            >
                {{ store.unreadCount > 9 ? "9+" : store.unreadCount }}
            </span>
        </button>

        <!-- Notification list panel -->
        <NotificationList v-if="isOpen" @close="isOpen = false" />
    </div>
</template>
