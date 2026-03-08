<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import * as LucideIcons from "lucide-vue-next";
import { ShoppingBag } from "lucide-vue-next";
import { format, parseISO } from "date-fns";
import type { Transaction } from "@/stores/transactions";
import { Skeleton } from "@/components/ui/skeleton";

const props = defineProps<{
    transactions: Transaction[];
    isLoading: boolean;
}>();

const emit = defineEmits<{
    (e: "edit", id: string): void;
    (e: "delete", id: string): void;
}>();

const { t } = useI18n();

interface Group {
    date: string;
    items: Transaction[];
    dailyTotal: number;
}

const grouped = computed<Group[]>(() => {
    const map = new Map<string, Transaction[]>();
    for (const tx of props.transactions) {
        if (!map.has(tx.date)) map.set(tx.date, []);
        map.get(tx.date)!.push(tx);
    }
    return [...map.entries()].map(([date, items]) => ({
        date,
        items,
        dailyTotal: items.reduce((sum, tx) => {
            if (tx.type === "Income") return sum + tx.amount;
            if (tx.type === "Expense") return sum - tx.amount;
            return sum;
        }, 0),
    }));
});

function getIcon(name: string | null | undefined) {
    if (!name) return ShoppingBag;
    return (LucideIcons as Record<string, unknown>)[name] ?? ShoppingBag;
}

function amountClass(tx: Transaction): string {
    if (tx.type === "Income") return "text-income";
    if (tx.type === "Expense") return "text-expense";
    return "text-foreground";
}

function amountSign(tx: Transaction): string {
    if (tx.type === "Income") return "+";
    if (tx.type === "Expense") return "−";
    return "";
}
</script>

<template>
    <div v-if="isLoading" class="space-y-2 px-4 pt-4">
        <Skeleton v-for="i in 4" :key="i" class="h-14 w-full rounded-lg" />
    </div>

    <template v-else>
        <p v-if="transactions.length === 0" class="px-4 py-8 text-center text-muted-foreground">
            {{ t("transactions.empty") }}
        </p>

        <div v-else>
            <template v-for="group in grouped" :key="group.date">
                <!-- Date group header -->
                <div class="flex items-baseline gap-2 px-4 pt-5 pb-2">
                    <span class="text-3xl font-bold tabular-nums text-foreground leading-none">
                        {{ format(parseISO(group.date), "d") }}
                    </span>
                    <span class="text-xs text-muted-foreground uppercase tracking-wide flex-1">
                        {{ format(parseISO(group.date), "EEE · MMM yyyy") }}
                    </span>
                    <span
                        class="text-sm font-semibold tabular-nums"
                        :class="group.dailyTotal >= 0 ? 'text-income' : 'text-expense'"
                    >
                        {{ group.dailyTotal >= 0 ? "+" : "−"
                        }}{{ Math.abs(group.dailyTotal).toFixed(2) }}
                    </span>
                </div>

                <!-- Transaction rows -->
                <button
                    v-for="tx in group.items"
                    :key="tx.id"
                    class="flex w-full items-center gap-3 px-4 py-3 hover:bg-muted/50 active:bg-muted/70 text-left"
                    @click="emit('edit', tx.id)"
                >
                    <!-- Category circle with icon -->
                    <div
                        class="flex size-10 shrink-0 items-center justify-center rounded-full"
                        :style="{ backgroundColor: tx.categoryColor ?? 'oklch(0.556 0.127 177.5)' }"
                    >
                        <component :is="getIcon(tx.categoryIcon)" class="size-4 text-white" />
                    </div>

                    <!-- Middle: category name + wallet + note -->
                    <div class="flex min-w-0 flex-1 flex-col">
                        <span class="truncate text-sm font-medium">{{ tx.categoryName }}</span>
                        <div class="flex items-center gap-1 mt-0.5">
                            <!-- Wallet color chip -->
                            <span
                                v-if="tx.walletName"
                                class="inline-flex size-3.5 shrink-0 rounded-sm"
                                :style="{
                                    backgroundColor: tx.categoryColor
                                        ? tx.categoryColor + '60'
                                        : 'oklch(0.556 0.127 177.5 / 0.38)',
                                }"
                            />
                            <span
                                v-if="tx.walletName"
                                class="truncate text-xs text-muted-foreground"
                            >
                                {{ tx.walletName }}
                            </span>
                            <template v-if="tx.walletName && tx.description">
                                <span class="text-xs text-muted-foreground">·</span>
                            </template>
                            <span
                                v-if="tx.description"
                                class="truncate text-xs italic text-muted-foreground"
                            >
                                {{ tx.description }}
                            </span>
                        </div>
                    </div>

                    <!-- Amount -->
                    <span
                        class="shrink-0 text-sm font-semibold tabular-nums"
                        :class="amountClass(tx)"
                    >
                        {{ amountSign(tx) }}{{ tx.amount.toFixed(2) }}
                    </span>
                </button>
            </template>
        </div>
    </template>
</template>
