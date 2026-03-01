<script setup lang="ts">
import { computed } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { Bell, LogOut, Settings } from "lucide-vue-next";
import { useAuthStore } from "@/stores/auth";
import { useSidebar } from "@/components/ui/sidebar";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem } from "@/components/ui/sidebar";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const { isMobile } = useSidebar();

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

const displayName = computed(() => auth.user?.name ?? auth.user?.email ?? "");

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
