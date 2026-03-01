<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { TrendingDown, TrendingUp } from "lucide-vue-next";
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
    CardAction,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useWalletsStore } from "@/stores/wallets";

interface SectionMetrics {
    incomeThisMonth: number;
    expensesThisMonth: number;
    netSavings: number;
    incomeTrend: number;
    expensesTrend: number;
    netTrend: number;
}

const props = defineProps<{
    metrics: SectionMetrics;
    isLoading: boolean;
}>();

const { t } = useI18n();
const walletsStore = useWalletsStore();

// Use the currency of the first active wallet, falling back to USD.
const currency = computed(() => walletsStore.activeWallets[0]?.currency ?? "USD");

// Format a number using the wallet currency via Intl.NumberFormat.
function formatAmount(amount: number): string {
    return new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: currency.value,
        minimumFractionDigits: 2,
    }).format(amount);
}

const trendPercent = (trend: number) => `${trend >= 0 ? "+" : ""}${trend.toFixed(1)}%`;

const cards = computed(() => [
    {
        key: "incomeThisMonth",
        label: t("dashboard.sectionCards.incomeThisMonth"),
        value: props.metrics.incomeThisMonth,
        trend: props.metrics.incomeTrend,
    },
    {
        key: "expensesThisMonth",
        label: t("dashboard.sectionCards.expensesThisMonth"),
        value: props.metrics.expensesThisMonth,
        trend: props.metrics.expensesTrend,
    },
    {
        key: "netSavings",
        label: t("dashboard.sectionCards.netSavings"),
        value: props.metrics.netSavings,
        trend: props.metrics.netTrend,
    },
]);
</script>

<template>
    <div class="grid grid-cols-1 gap-4 px-4 @xl/main:grid-cols-2 @5xl/main:grid-cols-3 lg:px-6">
        <Card v-for="card in cards" :key="card.key">
            <CardHeader class="pb-2">
                <div class="flex items-start justify-between">
                    <CardDescription>{{ card.label }}</CardDescription>
                    <CardAction>
                        <Badge :variant="card.trend >= 0 ? 'default' : 'destructive'" class="gap-1">
                            <TrendingUp v-if="card.trend >= 0" class="size-3" />
                            <TrendingDown v-else class="size-3" />
                            {{ trendPercent(card.trend) }}
                        </Badge>
                    </CardAction>
                </div>
                <Skeleton v-if="props.isLoading" class="h-7 w-32" />
                <CardTitle v-else class="text-2xl font-bold tabular-nums">
                    {{ formatAmount(card.value) }}
                </CardTitle>
            </CardHeader>
            <CardContent> </CardContent>
            <CardFooter class="text-xs text-muted-foreground">
                {{ t("dashboard.sectionCards.vsLastMonth") }}
            </CardFooter>
        </Card>
    </div>
</template>
