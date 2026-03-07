<script setup lang="ts">
import { computed } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { Bell, LogOut, Moon, Settings, Sun } from "lucide-vue-next";
import { useAuthStore } from "@/stores/auth";
import { useSidebar } from "@/components/ui/sidebar";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem } from "@/components/ui/sidebar";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const { t, locale } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const { isMobile } = useSidebar();

const appVersion = import.meta.env.VITE_APP_VERSION as string;

// Compute initials from name (up to 2 chars) or fallback to first letter of email.
const initials = computed(() => {
    const user = auth.user;
    if (!user) return "?";
    if (user.name) {
        return user.name
            .split(" ")
            .map((s) => s[0])
            .join("")
            .toUpperCase()
            .slice(0, 2);
    }
    return user.email[0].toUpperCase();
});

const displayName = computed(
    () => auth.user?.name ?? auth.user?.username ?? auth.user?.email ?? "",
);

// Theme toggle — persists to localStorage and applies class on <html>
function isDark(): boolean {
    return document.documentElement.classList.contains("dark");
}

function toggleTheme() {
    const html = document.documentElement;
    if (isDark()) {
        html.classList.remove("dark");
        localStorage.setItem("theme", "light");
    } else {
        html.classList.add("dark");
        localStorage.setItem("theme", "dark");
    }
}

// Language toggle — persists to localStorage
function setLocale(lang: string) {
    locale.value = lang;
    localStorage.setItem("locale", lang);
}

async function handleLogout() {
    await auth.logout();
    router.push({ name: "login" });
}
</script>

<template>
    <SidebarMenu>
        <SidebarMenuItem>
            <DropdownMenu>
                <DropdownMenuTrigger as-child>
                    <SidebarMenuButton
                        size="lg"
                        class="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
                    >
                        <Avatar class="size-8 rounded-lg">
                            <AvatarImage v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" />
                            <AvatarFallback class="rounded-lg">{{ initials }}</AvatarFallback>
                        </Avatar>
                        <div class="grid flex-1 text-left text-sm leading-tight">
                            <span class="truncate font-semibold">{{ displayName }}</span>
                            <span class="truncate text-xs text-muted-foreground">{{
                                auth.user?.email
                            }}</span>
                        </div>
                    </SidebarMenuButton>
                </DropdownMenuTrigger>
                <DropdownMenuContent
                    class="w-56"
                    align="end"
                    :side="isMobile ? 'bottom' : 'right'"
                    :side-offset="4"
                >
                    <!-- Quick preferences -->
                    <DropdownMenuLabel class="text-xs text-muted-foreground font-normal">
                        {{ t("nav.preferences") }}
                    </DropdownMenuLabel>

                    <!-- Language toggle -->
                    <DropdownMenuItem as-child>
                        <div class="flex items-center justify-between px-2 py-1.5 cursor-default">
                            <span class="text-sm">{{ t("nav.language") }}</span>
                            <div class="flex gap-1">
                                <button
                                    class="rounded px-1.5 py-0.5 text-xs font-medium transition-colors"
                                    :class="
                                        locale === 'en'
                                            ? 'bg-primary text-primary-foreground'
                                            : 'hover:bg-muted'
                                    "
                                    @click.stop="setLocale('en')"
                                >
                                    EN
                                </button>
                                <button
                                    class="rounded px-1.5 py-0.5 text-xs font-medium transition-colors"
                                    :class="
                                        locale === 'es'
                                            ? 'bg-primary text-primary-foreground'
                                            : 'hover:bg-muted'
                                    "
                                    @click.stop="setLocale('es')"
                                >
                                    ES
                                </button>
                            </div>
                        </div>
                    </DropdownMenuItem>

                    <!-- Theme toggle -->
                    <DropdownMenuItem @click="toggleTheme">
                        <Sun v-if="isDark()" class="mr-2 size-4" />
                        <Moon v-else class="mr-2 size-4" />
                        {{ isDark() ? t("nav.lightMode") : t("nav.darkMode") }}
                    </DropdownMenuItem>

                    <!-- App version -->
                    <DropdownMenuItem disabled class="text-xs text-muted-foreground">
                        v{{ appVersion }}
                    </DropdownMenuItem>

                    <DropdownMenuSeparator />

                    <DropdownMenuItem as-child>
                        <router-link :to="{ name: 'settings' }">
                            <Settings class="mr-2 size-4" />
                            {{ t("settings.title") }}
                        </router-link>
                    </DropdownMenuItem>
                    <DropdownMenuItem as-child>
                        <router-link :to="{ name: 'notification-preferences' }">
                            <Bell class="mr-2 size-4" />
                            {{ t("notifications.preferences") }}
                        </router-link>
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem @click="handleLogout">
                        <LogOut class="mr-2 size-4" />
                        {{ t("auth.logout") }}
                    </DropdownMenuItem>
                </DropdownMenuContent>
            </DropdownMenu>
        </SidebarMenuItem>
    </SidebarMenu>
</template>
