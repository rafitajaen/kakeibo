<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbList,
    BreadcrumbPage,
} from "@/components/ui/breadcrumb";

const { t } = useI18n();
const route = useRoute();

const routeTitleMap: Record<string, string> = {
    home: "dashboard.title",
    wallets: "wallets.title",
    "wallets-create": "wallets.createNew",
    "wallet-detail": "wallets.title",
    "wallet-edit": "wallets.form.submitEdit",
    "wallet-members": "wallets.members.title",
    "wallet-invite": "wallets.invitation.title",
    budgets: "app.budgets",
    "budget-create": "wallets.budgets.createNew",
    "budget-edit": "wallets.budgets.actions.edit",
    goals: "app.goals",
    "goal-create": "wallets.goals.createNew",
    "goal-edit": "wallets.goals.actions.edit",
    recurring: "app.recurring",
    "recurring-create": "wallets.recurring.createNew",
    "recurring-edit": "wallets.recurring.actions.edit",
    categories: "categories.title",
    "category-create": "categories.createNew",
    "category-edit": "categories.actions.edit",
    "notification-preferences": "notifications.prefs.title",
    activity: "activity.title",
    settings: "settings.title",
};

const pageTitle = computed(() => {
    const key = routeTitleMap[route.name as string];
    return key ? t(key) : t("app.name");
});
</script>

<template>
    <header
        class="flex h-12 shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear group-has-[[data-collapsible=icon]]/sidebar-wrapper:h-12"
    >
        <div class="flex items-center gap-2 px-4">
            <SidebarTrigger class="-ml-1" />
            <Separator orientation="vertical" class="mr-2 h-4" />
            <Breadcrumb>
                <BreadcrumbList>
                    <BreadcrumbItem>
                        <BreadcrumbPage>{{ pageTitle }}</BreadcrumbPage>
                    </BreadcrumbItem>
                </BreadcrumbList>
            </Breadcrumb>
        </div>
    </header>
</template>
