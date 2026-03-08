<script setup lang="ts">
import { useI18n } from "vue-i18n";
import type { Budget } from "@/stores/budgets";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import BudgetProgressBar from "@/components/budgets/BudgetProgressBar.vue";
import BudgetStatusBadge from "@/components/budgets/BudgetStatusBadge.vue";

defineProps<{
    budgets: Budget[];
    isLoading: boolean;
}>();

const emit = defineEmits<{
    (e: "edit", id: string): void;
    (e: "delete", id: string): void;
}>();

const { t } = useI18n();

// Compute remaining for each budget (capped at 0)
function remaining(budget: Budget): number {
    return Math.max(0, budget.limit - budget.currentSpending);
}
</script>

<template>
    <div class="space-y-3">
        <div v-if="isLoading" class="space-y-3">
            <Skeleton v-for="i in 3" :key="i" class="h-24 w-full rounded-lg" />
        </div>

        <p v-else-if="budgets.length === 0" class="text-sm text-muted-foreground">
            {{ t("wallets.budgets.empty") }}
        </p>

        <Card v-for="budget in budgets" :key="budget.id">
            <CardContent class="pt-4">
                <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                        <div class="flex flex-wrap items-center gap-2">
                            <span class="font-medium">{{ budget.name }}</span>
                            <BudgetStatusBadge
                                :current-spending="budget.currentSpending"
                                :limit="budget.limit"
                            />
                        </div>

                        <p class="mt-1 text-sm text-muted-foreground">
                            {{ budget.categoryName }}
                            <span v-if="budget.walletName"> · {{ budget.walletName }}</span>
                        </p>

                        <p class="mt-1 text-sm text-muted-foreground">
                            {{ budget.startDate }} – {{ budget.endDate }}
                        </p>

                        <!-- Progress bar -->
                        <BudgetProgressBar
                            class="mt-2"
                            :current-spending="budget.currentSpending"
                            :limit="budget.limit"
                        />

                        <!-- Spending detail -->
                        <div class="mt-1 flex justify-between text-sm">
                            <span>
                                {{ t("wallets.budgets.detail.spent") }}:
                                {{ budget.currentSpending.toFixed(2) }} /
                                {{ budget.limit.toFixed(2) }}
                            </span>
                            <span class="text-muted-foreground">
                                {{ t("wallets.budgets.detail.remaining") }}:
                                {{ remaining(budget).toFixed(2) }}
                            </span>
                        </div>
                    </div>

                    <!-- Actions -->
                    <div class="flex shrink-0 gap-1">
                        <Button variant="ghost" size="sm" @click="emit('edit', budget.id)">
                            {{ t("wallets.budgets.actions.edit") }}
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            class="text-destructive hover:text-destructive"
                            @click="emit('delete', budget.id)"
                        >
                            {{ t("wallets.budgets.actions.delete") }}
                        </Button>
                    </div>
                </div>
            </CardContent>
        </Card>
    </div>
</template>
