<script setup lang="ts">
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { PiggyBank, Target, Repeat, Users, Activity, Settings, Bell } from "lucide-vue-next";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";

const open = defineModel<boolean>("open", { default: false });

const { t } = useI18n();
const router = useRouter();

const items = [
    { icon: PiggyBank, label: () => t("app.budgets"), route: "budgets" },
    { icon: Target, label: () => t("app.goals"), route: "goals" },
    { icon: Repeat, label: () => t("app.recurring"), route: "recurring" },
    { icon: Users, label: () => t("friends.title"), route: "friends" },
    { icon: Activity, label: () => t("activity.title"), route: "activity" },
    { icon: Settings, label: () => t("settings.title"), route: "settings" },
    { icon: Bell, label: () => t("notifications.preferences"), route: "notification-preferences" },
];

function navigate(routeName: string) {
    open.value = false;
    router.push({ name: routeName });
}
</script>

<template>
    <Sheet v-model:open="open">
        <SheetContent side="bottom" class="rounded-t-xl">
            <SheetHeader class="mb-4">
                <SheetTitle>{{ t("nav.tabs.more") }}</SheetTitle>
            </SheetHeader>
            <div class="grid grid-cols-3 gap-4 pb-4">
                <button
                    v-for="item in items"
                    :key="item.route"
                    class="flex flex-col items-center gap-2 p-3 rounded-xl hover:bg-muted transition-colors"
                    @click="navigate(item.route)"
                >
                    <div
                        class="flex size-12 items-center justify-center rounded-full bg-primary/10"
                    >
                        <component :is="item.icon" class="size-6 text-primary" />
                    </div>
                    <span class="text-xs text-center leading-tight">{{ item.label() }}</span>
                </button>
            </div>
        </SheetContent>
    </Sheet>
</template>
