<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { RouterLink } from "vue-router";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import type { Goal } from "@/stores/goals";

defineProps<{
    goals: Goal[];
    isLoading: boolean;
}>();

const { t } = useI18n();

function percentageComplete(goal: Goal): number {
    if (goal.targetAmount === 0) return 0;
    return Math.min(Math.round((goal.currentProgress / goal.targetAmount) * 100), 100);
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("dashboard.goalSummary.title") }}</CardTitle>
        </CardHeader>
        <CardContent>
            <p v-if="isLoading" class="text-muted-foreground">{{ t("common.loading") }}</p>
            <div v-else-if="goals.length === 0" class="text-sm text-muted-foreground">
                <p>{{ t("dashboard.goalSummary.empty") }}</p>
                <RouterLink
                    :to="{ name: 'goal-create' }"
                    class="text-primary text-sm mt-1 inline-block"
                >
                    {{ t("dashboard.goalSummary.createNew") }}
                </RouterLink>
            </div>
            <div v-else class="space-y-3">
                <RouterLink
                    v-for="goal in goals"
                    :key="goal.id"
                    :to="{ name: 'goal-edit', params: { id: goal.id } }"
                    class="block"
                >
                    <div class="flex justify-between items-center text-sm mb-1">
                        <span class="font-medium truncate">{{ goal.name }}</span>
                        <span class="text-muted-foreground ml-2 shrink-0"
                            >{{ percentageComplete(goal) }}%</span
                        >
                    </div>
                    <Progress :model-value="percentageComplete(goal)" class="h-2" />
                </RouterLink>
            </div>
        </CardContent>
    </Card>
</template>
