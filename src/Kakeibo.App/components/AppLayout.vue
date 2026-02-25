<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import NotificationBell from "@/components/notifications/NotificationBell.vue";

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const navLinks = [
    { name: "home", label: "app.home" },
    { name: "wallets", label: "wallets.title" },
    { name: "budgets", label: "app.budgets" },
    { name: "goals", label: "app.goals" },
    { name: "recurring", label: "app.recurring" },
    { name: "activity", label: "activity.title" },
];

async function handleLogout() {
    await auth.logout();
    router.push({ name: "login" });
}
</script>

<template>
    <div class="min-h-screen bg-background flex flex-col">
        <!-- Header -->
        <header class="border-b bg-background sticky top-0 z-40">
            <div class="container flex h-14 items-center gap-4">
                <router-link :to="{ name: 'home' }" class="font-bold text-lg"> 家計簿 </router-link>

                <!-- Desktop nav -->
                <nav class="hidden md:flex gap-4 flex-1">
                    <router-link
                        v-for="link in navLinks"
                        :key="link.name"
                        :to="{ name: link.name }"
                        class="text-sm text-muted-foreground hover:text-foreground transition-colors"
                        active-class="text-foreground font-medium"
                    >
                        {{ t(link.label) }}
                    </router-link>
                </nav>

                <div class="ml-auto flex items-center gap-2">
                    <NotificationBell />
                    <button
                        class="text-sm text-muted-foreground hover:text-foreground"
                        @click="handleLogout"
                    >
                        {{ t("auth.logout") }}
                    </button>
                </div>
            </div>
        </header>

        <!-- Main content -->
        <main class="flex-1">
            <RouterView />
        </main>
    </div>
</template>
