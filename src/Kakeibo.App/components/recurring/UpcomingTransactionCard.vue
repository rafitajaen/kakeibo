<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { format, parseISO } from "date-fns";
import FrequencyBadge from "@/components/recurring/FrequencyBadge.vue";
import type { ForecastItem } from "@/stores/recurring";

defineProps<{
    item: ForecastItem;
}>();

const { t } = useI18n();

// Formats a YYYY-MM-DD date string for display (e.g. "Mar 15, 2026").
function formatDate(date: string): string {
    return format(parseISO(date), "PP");
}
</script>

<template>
    <div
        class="flex items-center justify-between border rounded-lg p-3"
        data-testid="forecast-item"
    >
        <div class="space-y-1">
            <div class="flex items-center gap-2">
                <span class="font-medium text-sm">{{ item.patternName }}</span>
                <FrequencyBadge :frequency="item.frequency" />
            </div>
            <div class="text-xs text-muted-foreground">
                {{ item.categoryName }} &middot; {{ item.walletName }}
            </div>
        </div>
        <div class="text-right space-y-1">
            <div
                class="font-medium text-sm"
                :class="{
                    'text-green-600': item.transactionType === 'Income',
                    'text-destructive': item.transactionType === 'Expense',
                    'text-muted-foreground': item.transactionType === 'Transfer',
                }"
            >
                {{
                    item.transactionType === "Income"
                        ? "+"
                        : item.transactionType === "Expense"
                          ? "-"
                          : ""
                }}{{ item.amount.toFixed(2) }}
            </div>
            <div class="text-xs text-muted-foreground">
                {{ formatDate(item.date) }}
            </div>
        </div>
    </div>
</template>
