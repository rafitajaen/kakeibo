<script setup lang="ts">
import { useI18n } from "vue-i18n";
import type { Budget } from "@/stores/budgets";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
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
</script>

<template>
    <div class="space-y-3">
        <p v-if="isLoading" class="text-sm text-muted-foreground">{{ t("common.loading") }}</p>

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

                        <p class="mt-1 text-sm">
                            {{ t("wallets.budgets.detail.spent") }}:
                            {{ budget.currentSpending.toFixed(2) }} /
                            {{ budget.limit.toFixed(2) }}
                        </p>
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
