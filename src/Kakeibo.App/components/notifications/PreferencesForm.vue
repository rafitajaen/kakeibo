<script setup lang="ts">
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import { useNotificationsStore } from "@/stores/notifications";
import type { NotificationPreferences } from "@/stores/notifications";

const { t } = useI18n();
const store = useNotificationsStore();

// Local copy of preferences — only submitted on explicit save.
const local = ref<NotificationPreferences>({
    emailBudgetAlerts: true,
    emailGoalMilestones: true,
    emailInvitations: true,
    emailRecurringUpdates: true,
    pushBudgetAlerts: true,
    pushGoalMilestones: true,
    pushInvitations: true,
    pushRecurringUpdates: true,
});

const isSaving = ref(false);
const saved = ref(false);

// Sync local state when store preferences are loaded.
watch(
    () => store.preferences,
    (prefs) => {
        if (prefs) {
            local.value = { ...prefs };
        }
    },
    { immediate: true },
);

async function save() {
    isSaving.value = true;
    saved.value = false;
    try {
        await store.updatePreferences(local.value);
        saved.value = true;
    } finally {
        isSaving.value = false;
    }
}
</script>

<template>
    <div class="space-y-6">
        <h2 class="text-lg font-semibold">{{ t("notifications.prefs.title") }}</h2>

        <!-- Email preferences -->
        <section class="space-y-3">
            <h3 class="text-sm font-medium text-muted-foreground">
                {{ t("notifications.prefs.emailAlerts") }}
            </h3>
            <div class="space-y-2">
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.emailBudgetAlerts" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.budgetAlerts") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.emailGoalMilestones" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.goalMilestones") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.emailInvitations" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.invitations") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.emailRecurringUpdates" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.recurringUpdates") }}</span>
                </label>
            </div>
        </section>

        <!-- Push preferences -->
        <section class="space-y-3">
            <h3 class="text-sm font-medium text-muted-foreground">
                {{ t("notifications.prefs.pushAlerts") }}
            </h3>
            <div class="space-y-2">
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.pushBudgetAlerts" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.budgetAlerts") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.pushGoalMilestones" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.goalMilestones") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.pushInvitations" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.invitations") }}</span>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                    <input v-model="local.pushRecurringUpdates" type="checkbox" class="h-4 w-4" />
                    <span class="text-sm">{{ t("notifications.prefs.recurringUpdates") }}</span>
                </label>
            </div>
        </section>

        <div class="flex items-center gap-3">
            <Button :disabled="isSaving" @click="save">
                {{ t("notifications.prefs.save") }}
            </Button>
            <span v-if="saved" class="text-sm text-green-600">
                {{ t("notifications.prefs.saved") }}
            </span>
        </div>
    </div>
</template>
