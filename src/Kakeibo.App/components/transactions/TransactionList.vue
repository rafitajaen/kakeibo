<script setup lang="ts">
import { useI18n } from "vue-i18n";
import type { Transaction } from "@/stores/transactions";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

defineProps<{
    transactions: Transaction[];
    isLoading: boolean;
}>();

const emit = defineEmits<{
    (e: "edit", id: string): void;
    (e: "delete", id: string): void;
}>();

const { t } = useI18n();
</script>

<template>
    <p v-if="isLoading" class="text-muted-foreground">{{ t("common.loading") }}</p>

    <template v-else>
        <p v-if="transactions.length === 0" class="text-muted-foreground">
            {{ t("transactions.empty") }}
        </p>

        <div v-else class="space-y-2">
            <Card v-for="tx in transactions" :key="tx.id" class="cursor-pointer hover:bg-muted/50">
                <CardHeader class="flex flex-row items-center justify-between py-3">
                    <div class="flex items-center gap-2">
                        <Badge
                            :variant="
                                tx.type === 'Income'
                                    ? 'default'
                                    : tx.type === 'Expense'
                                      ? 'destructive'
                                      : 'secondary'
                            "
                        >
                            {{ t(`transactions.type.${tx.type.toLowerCase()}`) }}
                        </Badge>
                        <CardTitle class="text-base">{{ tx.description }}</CardTitle>
                    </div>
                    <span
                        :class="[
                            'text-lg font-semibold tabular-nums',
                            tx.type === 'Income'
                                ? 'text-green-600'
                                : tx.type === 'Expense'
                                  ? 'text-destructive'
                                  : 'text-foreground',
                        ]"
                    >
                        {{ tx.type === "Income" ? "+" : tx.type === "Expense" ? "-" : ""
                        }}{{ tx.amount.toFixed(2) }}
                    </span>
                </CardHeader>
                <CardContent class="flex items-center justify-between py-2 pt-0">
                    <div class="flex gap-3 text-sm text-muted-foreground">
                        <span>{{ tx.date }}</span>
                        <span>{{ tx.categoryName }}</span>
                    </div>
                    <div class="flex gap-2">
                        <Button variant="ghost" size="sm" @click="emit('edit', tx.id)">
                            {{ t("common.edit") }}
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            class="text-destructive hover:text-destructive"
                            @click="emit('delete', tx.id)"
                        >
                            {{ t("common.delete") }}
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    </template>
</template>
