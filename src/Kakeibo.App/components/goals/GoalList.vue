<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { format, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import GoalMilestoneIndicator from "@/components/goals/GoalMilestoneIndicator.vue";
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

// Formats a YYYY-MM-DD deadline string for display (e.g. "Dec 31, 2026").
function formatDeadline(deadline: string): string {
    return format(parseISO(deadline), "PP");
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

                    <!-- AlertDialog replaces native confirm() for delete confirmation -->
                    <AlertDialog>
                        <AlertDialogTrigger as-child>
                            <Button variant="destructive" size="sm">
                                {{ t("wallets.goals.actions.delete") }}
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>
                                    {{ t("wallets.goals.actions.deleteTitle") }}
                                </AlertDialogTitle>
                                <AlertDialogDescription>
                                    {{ t("wallets.goals.actions.deleteConfirm") }}
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                                <AlertDialogAction
                                    data-testid="delete-confirm"
                                    @click="emit('delete', goal.id)"
                                >
                                    {{ t("wallets.goals.actions.delete") }}
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
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
                    {{ t("wallets.goals.detail.deadline") }}: {{ formatDeadline(goal.deadline) }}
                </span>
            </div>

            <GoalMilestoneIndicator
                :last-milestone="goal.lastMilestone"
                :target-amount="goal.targetAmount"
                :current-progress="goal.currentProgress"
            />

            <div class="text-xs text-muted-foreground">
                {{ t("wallets.goals.detail.wallet") }}: {{ goal.walletName }}
            </div>
        </div>
    </div>
</template>
