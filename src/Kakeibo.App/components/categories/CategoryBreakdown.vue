<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
import { ShoppingBag } from "lucide-vue-next";
import { computed } from "vue";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

export interface BreakdownItem {
    categoryId: string;
    name: string;
    color?: string | null;
    icon?: string | null;
    amount: number;
    total: number;
}

const props = defineProps<{
    items: BreakdownItem[];
}>();

const { formatCurrency } = useCurrencyFormat();

const sorted = computed(() => [...props.items].sort((a, b) => b.amount - a.amount));

function getIcon(name: string | null | undefined) {
    if (!name) return ShoppingBag;
    return (LucideIcons as Record<string, unknown>)[name] ?? ShoppingBag;
}

function percentage(item: BreakdownItem): number {
    if (item.total === 0) return 0;
    return Math.min(100, Math.round((item.amount / item.total) * 100));
}
</script>

<template>
    <div class="divide-y divide-border">
        <div
            v-for="item in sorted"
            :key="item.categoryId"
            class="flex items-center gap-3 px-4 py-3"
        >
            <!-- Icon circle -->
            <div
                class="flex size-10 shrink-0 items-center justify-center rounded-full"
                :style="{ backgroundColor: item.color ?? 'oklch(0.556 0.127 177.5)' }"
            >
                <component :is="getIcon(item.icon)" class="size-4 text-white" />
            </div>

            <!-- Name + progress bar -->
            <div class="flex min-w-0 flex-1 flex-col gap-1">
                <span class="truncate text-sm font-medium">{{ item.name }}</span>
                <div class="h-1.5 w-full rounded-full bg-muted overflow-hidden">
                    <div
                        class="h-full rounded-full transition-all"
                        :style="{
                            width: `${percentage(item)}%`,
                            backgroundColor: item.color ?? 'oklch(0.556 0.127 177.5)',
                        }"
                    />
                </div>
            </div>

            <!-- Amount + percentage -->
            <div class="shrink-0 flex flex-col items-end">
                <span class="text-sm font-semibold tabular-nums text-expense">
                    {{ formatCurrency(item.amount) }}
                </span>
                <span class="text-xs text-muted-foreground">{{ percentage(item) }}%</span>
            </div>
        </div>
    </div>
</template>
