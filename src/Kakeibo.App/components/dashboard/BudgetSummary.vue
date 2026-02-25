<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { RouterLink } from "vue-router";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import type { Budget } from "@/stores/budgets";

defineProps<{
    budgets: Budget[];
    isLoading: boolean;
}>();

const { t } = useI18n();

function percentageUsed(budget: Budget): number {
    if (budget.limit === 0) return 0;
    return Math.min(Math.round((budget.currentSpending / budget.limit) * 100), 100);
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("dashboard.budgetSummary.title") }}</CardTitle>
        </CardHeader>
        <CardContent>
            <p v-if="isLoading" class="text-muted-foreground">{{ t("common.loading") }}</p>
            <div v-else-if="budgets.length === 0" class="text-sm text-muted-foreground">
                <p>{{ t("dashboard.budgetSummary.empty") }}</p>
                <RouterLink
                    :to="{ name: 'budget-create' }"
                    class="text-primary text-sm mt-1 inline-block"
                >
                    {{ t("dashboard.budgetSummary.createNew") }}
                </RouterLink>
            </div>
            <div v-else class="space-y-3">
                <RouterLink
                    v-for="budget in budgets"
                    :key="budget.id"
                    :to="{ name: 'budget-edit', params: { id: budget.id } }"
                    class="block"
                >
                    <div class="flex justify-between items-center text-sm mb-1">
                        <span class="font-medium truncate">{{ budget.name }}</span>
                        <span class="text-muted-foreground ml-2 shrink-0"
                            >{{ percentageUsed(budget) }}%</span
                        >
                    </div>
                    <Progress :model-value="percentageUsed(budget)" class="h-2" />
                </RouterLink>
            </div>
        </CardContent>
    </Card>
</template>
