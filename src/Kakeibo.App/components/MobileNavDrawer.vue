<script setup lang="ts">
import { watch } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import {
    X,
    Settings,
    Bell,
    Users,
    Activity,
    PiggyBank,
    Target,
    Repeat,
    LogOut,
    Sun,
    Moon,
} from "lucide-vue-next";
import { Sheet, SheetContent } from "@/components/ui/sheet";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Separator } from "@/components/ui/separator";
import { useUserNav } from "@/composables/useUserNav";

const open = defineModel<boolean>("open", { default: false });

const { t } = useI18n();
const route = useRoute();
const { initials, displayName, isDark, toggleTheme, setLocale, handleLogout, auth, locale } =
    useUserNav();

const appVersion = import.meta.env.VITE_APP_VERSION as string;

watch(
    () => route.name,
    () => {
        open.value = false;
    },
);

const navItems = [
    { icon: Settings, label: () => t("settings.title"), route: "settings" },
    {
        icon: Bell,
        label: () => t("notifications.preferences"),
        route: "notification-preferences",
    },
    { icon: Users, label: () => t("friends.title"), route: "friends" },
    { icon: Activity, label: () => t("activity.title"), route: "activity" },
    { icon: PiggyBank, label: () => t("app.budgets"), route: "budgets" },
    { icon: Target, label: () => t("app.goals"), route: "goals" },
    { icon: Repeat, label: () => t("app.recurring"), route: "recurring" },
];
</script>

<template>
    <Sheet v-model:open="open">
        <SheetContent side="left" class="w-72 p-0 flex flex-col">
            <!-- Header -->
            <div class="flex items-center justify-between px-4 py-3 border-b">
                <span class="font-bold text-lg text-primary">K Kakeibo</span>
                <button class="text-muted-foreground" @click="open = false">
                    <X class="size-5" />
                </button>
            </div>

            <!-- User info -->
            <div class="flex items-center gap-3 px-4 py-4">
                <Avatar class="size-10 rounded-lg">
                    <AvatarImage v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" />
                    <AvatarFallback class="rounded-lg">{{ initials }}</AvatarFallback>
                </Avatar>
                <div class="min-w-0">
                    <p class="font-semibold truncate text-sm">{{ displayName }}</p>
                    <p class="text-xs text-muted-foreground truncate">{{ auth.user?.email }}</p>
                </div>
            </div>

            <Separator />

            <!-- Nav items -->
            <nav class="flex-1 overflow-y-auto py-2">
                <router-link
                    v-for="item in navItems"
                    :key="item.route"
                    :to="{ name: item.route }"
                    class="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-muted transition-colors"
                    active-class="text-primary bg-primary/10"
                >
                    <component :is="item.icon" class="size-4 shrink-0" />
                    {{ item.label() }}
                </router-link>
            </nav>

            <Separator />

            <!-- Language + Theme -->
            <div class="px-4 py-3 space-y-3">
                <div class="flex items-center justify-between">
                    <span class="text-sm">{{ t("nav.language") }}</span>
                    <div class="flex gap-1">
                        <button
                            class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
                            :class="
                                locale === 'en'
                                    ? 'bg-primary text-primary-foreground'
                                    : 'hover:bg-muted'
                            "
                            @click="setLocale('en')"
                        >
                            EN
                        </button>
                        <button
                            class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
                            :class="
                                locale === 'es'
                                    ? 'bg-primary text-primary-foreground'
                                    : 'hover:bg-muted'
                            "
                            @click="setLocale('es')"
                        >
                            ES
                        </button>
                    </div>
                </div>
                <button
                    class="flex items-center gap-2 text-sm w-full hover:text-primary transition-colors"
                    @click="toggleTheme"
                >
                    <Sun v-if="isDark()" class="size-4" />
                    <Moon v-else class="size-4" />
                    {{ isDark() ? t("nav.lightMode") : t("nav.darkMode") }}
                </button>
            </div>

            <Separator />

            <!-- Logout + version -->
            <div class="px-4 py-3">
                <button
                    class="flex items-center gap-2 text-sm text-destructive w-full hover:text-destructive/80 transition-colors"
                    @click="handleLogout"
                >
                    <LogOut class="size-4" />
                    {{ t("auth.logout") }}
                </button>
                <p class="mt-3 text-xs text-muted-foreground">v{{ appVersion }}</p>
            </div>
        </SheetContent>
    </Sheet>
</template>
