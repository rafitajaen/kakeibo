<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { Activity, LayoutDashboard, PiggyBank, Repeat, Target, Wallet } from "lucide-vue-next";
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar";
import NavMain from "@/components/nav/NavMain.vue";
import NavUser from "@/components/nav/NavUser.vue";

const { t } = useI18n();

const navMainItems = [
    {
        title: computed(() => t("app.home")),
        routeName: "home",
        icon: LayoutDashboard,
    },
    {
        title: computed(() => t("wallets.title")),
        routeName: "wallets",
        icon: Wallet,
    },
    {
        title: computed(() => t("app.budgets")),
        routeName: "budgets",
        icon: PiggyBank,
    },
    {
        title: computed(() => t("app.goals")),
        routeName: "goals",
        icon: Target,
    },
    {
        title: computed(() => t("app.recurring")),
        routeName: "recurring",
        icon: Repeat,
    },
    {
        title: computed(() => t("activity.title")),
        routeName: "activity",
        icon: Activity,
    },
];
</script>

<template>
    <Sidebar collapsible="icon">
        <SidebarHeader>
            <SidebarMenu>
                <SidebarMenuItem>
                    <SidebarMenuButton as-child size="lg">
                        <router-link :to="{ name: 'home' }">
                            <span class="font-bold text-lg leading-tight">家計簿</span>
                            <span class="ml-2 font-semibold">Kakeibo</span>
                        </router-link>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarHeader>

        <SidebarContent>
            <NavMain :items="navMainItems.map((i) => ({ ...i, title: i.title.value }))" />
        </SidebarContent>

        <SidebarFooter>
            <NavUser />
        </SidebarFooter>
    </Sidebar>
</template>
