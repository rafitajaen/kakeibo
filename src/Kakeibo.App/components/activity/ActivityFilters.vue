<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { ActivityFilters } from "@/stores/activity";

const emit = defineEmits<{
    (e: "apply", filters: Partial<ActivityFilters>): void;
}>();

const { t } = useI18n();

const start = ref("");
const end = ref("");
const type = ref("");

const actionTypes = [
    "transaction.recorded",
    "transaction.updated",
    "transaction.deleted",
    "wallet.created",
    "wallet.archived",
    "budget.created",
    "goal.created",
    "invitation.sent",
    "invitation.accepted",
    "member.joined",
    "member.left",
    "settlement.recorded",
    "recurring.generated",
    "user.registered",
    "user.logged_in",
];

function apply() {
    emit("apply", {
        start: start.value || undefined,
        end: end.value || undefined,
        type: type.value || undefined,
        page: 1,
    });
}
</script>

<template>
    <div class="flex flex-wrap gap-3 items-end p-4 bg-muted/30 rounded-lg">
        <div class="flex flex-col gap-1">
            <label class="text-xs font-medium text-muted-foreground">
                {{ t("activity.filters.startDate") }}
            </label>
            <Input v-model="start" type="date" class="w-36" />
        </div>

        <div class="flex flex-col gap-1">
            <label class="text-xs font-medium text-muted-foreground">
                {{ t("activity.filters.endDate") }}
            </label>
            <Input v-model="end" type="date" class="w-36" />
        </div>

        <div class="flex flex-col gap-1">
            <label class="text-xs font-medium text-muted-foreground">
                {{ t("activity.filters.type") }}
            </label>
            <select
                v-model="type"
                class="h-9 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm"
            >
                <option value="">{{ t("activity.filters.all") }}</option>
                <option v-for="actionType in actionTypes" :key="actionType" :value="actionType">
                    {{ t(`activity.actions.${actionType}`) }}
                </option>
            </select>
        </div>

        <Button size="sm" @click="apply">{{ t("activity.filters.apply") }}</Button>
    </div>
</template>
