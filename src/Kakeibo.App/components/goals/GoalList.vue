<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Button } from "@/components/ui/button";
import GoalProgressBar from "@/components/goals/GoalProgressBar.vue";
import GoalStatusBadge from "@/components/goals/GoalStatusBadge.vue";
import type { Goal } from "@/stores/goals";

defineProps<{
    goals: Goal[];
}>();

const emit = defineEmits<{
    (e: "delete", id: string): void;
}>();

const { t } = useI18n();
const router = useRouter();

function handleEdit(id: string) {
    router.push({ name: "goal-edit", params: { id } });
}

function handleDelete(id: string) {
    if (confirm(t("wallets.goals.actions.deleteConfirm"))) {
        emit("delete", id);
    }
}
</script>

<template>
    <div class="space-y-4">
        <div
            v-for="goal in goals"
            :key="goal.id"
            class="border rounded-lg p-4 space-y-3"
            data-testid="goal-item"
        >
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                    <span class="font-medium">{{ goal.name }}</span>
                    <GoalStatusBadge
                        :current-progress="goal.currentProgress"
                        :target-amount="goal.targetAmount"
                    />
                </div>
                <div class="flex gap-2">
                    <Button variant="outline" size="sm" @click="handleEdit(goal.id)">
                        {{ t("wallets.goals.actions.edit") }}
                    </Button>
                    <Button variant="destructive" size="sm" @click="handleDelete(goal.id)">
                        {{ t("wallets.goals.actions.delete") }}
                    </Button>
                </div>
            </div>

            <GoalProgressBar
                :current-progress="goal.currentProgress"
                :target-amount="goal.targetAmount"
            />

            <div class="flex justify-between text-sm text-muted-foreground">
                <span>
                    {{ t("wallets.goals.detail.progress") }}:
                    {{ goal.currentProgress.toFixed(2) }} /
                    {{ goal.targetAmount.toFixed(2) }}
                </span>
                <span v-if="goal.deadline">
                    {{ t("wallets.goals.detail.deadline") }}: {{ goal.deadline }}
                </span>
            </div>

            <div class="text-xs text-muted-foreground">
                {{ t("wallets.goals.detail.wallet") }}: {{ goal.walletName }}
            </div>
        </div>
    </div>
</template>
