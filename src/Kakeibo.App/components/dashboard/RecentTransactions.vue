<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { RouterLink } from "vue-router";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import type { Transaction } from "@/stores/transactions";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

defineProps<{
    transactions: Transaction[];
    isLoading: boolean;
}>();

const { t } = useI18n();
const { formatCurrency } = useCurrencyFormat();

function typeColorClass(type: string): string {
    if (type === "Income") return "text-green-600";
    if (type === "Expense") return "text-red-600";
    return "text-blue-600";
}

function formatAmount(tx: Transaction): string {
    const prefix = tx.type === "Income" ? "+" : tx.type === "Expense" ? "-" : "";
    return `${prefix}${formatCurrency.value(tx.amount)}`;
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("dashboard.recentTransactions.title") }}</CardTitle>
        </CardHeader>
        <CardContent>
            <div v-if="isLoading" class="space-y-1">
                <div v-for="i in 5" :key="i" class="flex justify-between items-center py-1">
                    <Skeleton class="h-4 w-48" />
                    <Skeleton class="h-4 w-16" />
                </div>
            </div>
            <p v-else-if="transactions.length === 0" class="text-sm text-muted-foreground">
                {{ t("dashboard.recentTransactions.empty") }}
            </p>
            <div v-else class="space-y-1">
                <RouterLink
                    v-for="tx in transactions"
                    :key="tx.id"
                    :to="{ name: 'transactions', params: { walletId: tx.walletId } }"
                    class="flex justify-between items-center text-sm py-1 hover:bg-muted rounded px-1"
                >
                    <div class="min-w-0 flex-1">
                        <span class="font-medium truncate">{{ tx.description }}</span>
                        <span class="text-muted-foreground ml-2">{{ tx.categoryName }}</span>
                    </div>
                    <span :class="typeColorClass(tx.type)" class="font-medium ml-2 shrink-0">
                        {{ formatAmount(tx) }}
                    </span>
                </RouterLink>
            </div>
        </CardContent>
    </Card>
</template>
