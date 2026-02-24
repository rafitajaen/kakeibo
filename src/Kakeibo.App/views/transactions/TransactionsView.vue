<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useTransactionsStore } from "@/stores/transactions";
import { useCategoriesStore } from "@/stores/categories";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const transactionsStore = useTransactionsStore();
const categoriesStore = useCategoriesStore();

const walletId = route.params.walletId as string;

const apiError = ref<string | null>(null);

// Filter state
const filterFrom = ref("");
const filterTo = ref("");
const filterCategoryId = ref("");
const filterType = ref("");

const TRANSACTION_TYPES = ["Income", "Expense", "Transfer"] as const;

async function loadTransactions() {
    apiError.value = null;
    try {
        await transactionsStore.fetchTransactions({
            walletId,
            from: filterFrom.value || null,
            to: filterTo.value || null,
            categoryId: filterCategoryId.value || null,
            type: filterType.value || null,
        });
    } catch {
        apiError.value = t("transactions.errors.unexpected");
    }
}

async function handleDelete(id: string) {
    if (!confirm(t("common.delete") + "?")) return;
    try {
        await transactionsStore.deleteTransaction(id);
    } catch {
        apiError.value = t("transactions.errors.unexpected");
    }
}

onMounted(async () => {
    await Promise.all([loadTransactions(), categoriesStore.fetchCategories()]);
});
</script>

<template>
    <div class="container mx-auto max-w-3xl px-4 py-8">
        <div class="mb-6 flex items-center justify-between">
            <h1 class="text-2xl font-semibold">{{ t("transactions.title") }}</h1>
            <Button @click="router.push({ name: 'transaction-record', params: { walletId } })">
                {{ t("transactions.createNew") }}
            </Button>
        </div>

        <!-- Filters -->
        <div class="mb-4 grid grid-cols-2 gap-2 sm:grid-cols-4">
            <div>
                <label class="mb-1 block text-xs text-muted-foreground">
                    {{ t("transactions.filters.dateFrom") }}
                </label>
                <Input type="date" v-model="filterFrom" @change="loadTransactions" />
            </div>
            <div>
                <label class="mb-1 block text-xs text-muted-foreground">
                    {{ t("transactions.filters.dateTo") }}
                </label>
                <Input type="date" v-model="filterTo" @change="loadTransactions" />
            </div>
            <div>
                <label class="mb-1 block text-xs text-muted-foreground">
                    {{ t("transactions.filters.allTypes") }}
                </label>
                <Select v-model="filterType" @update:model-value="loadTransactions">
                    <SelectTrigger>
                        <SelectValue :placeholder="t('transactions.filters.allTypes')" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="">{{ t("transactions.filters.allTypes") }}</SelectItem>
                        <SelectItem
                            v-for="txType in TRANSACTION_TYPES"
                            :key="txType"
                            :value="txType"
                        >
                            {{ t(`transactions.type.${txType.toLowerCase()}`) }}
                        </SelectItem>
                    </SelectContent>
                </Select>
            </div>
            <div>
                <label class="mb-1 block text-xs text-muted-foreground">
                    {{ t("transactions.filters.allCategories") }}
                </label>
                <Select v-model="filterCategoryId" @update:model-value="loadTransactions">
                    <SelectTrigger>
                        <SelectValue :placeholder="t('transactions.filters.allCategories')" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="">
                            {{ t("transactions.filters.allCategories") }}
                        </SelectItem>
                        <SelectItem
                            v-for="cat in categoriesStore.activeCategories"
                            :key="cat.id"
                            :value="cat.id"
                        >
                            {{ cat.name }}
                        </SelectItem>
                    </SelectContent>
                </Select>
            </div>
        </div>

        <p v-if="apiError" class="mb-4 text-destructive">{{ apiError }}</p>

        <p v-if="transactionsStore.isLoading" class="text-muted-foreground">
            {{ t("common.loading") }}
        </p>

        <template v-else>
            <p v-if="transactionsStore.transactions.length === 0" class="text-muted-foreground">
                {{ t("transactions.empty") }}
            </p>

            <div v-else class="space-y-2">
                <Card
                    v-for="tx in transactionsStore.transactions"
                    :key="tx.id"
                    class="cursor-pointer hover:bg-muted/50"
                >
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
                            <Button
                                variant="ghost"
                                size="sm"
                                @click="
                                    router.push({
                                        name: 'transaction-edit',
                                        params: { walletId, id: tx.id },
                                    })
                                "
                            >
                                {{ t("common.edit") }}
                            </Button>
                            <Button
                                variant="ghost"
                                size="sm"
                                class="text-destructive hover:text-destructive"
                                @click="handleDelete(tx.id)"
                            >
                                {{ t("common.delete") }}
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </template>
    </div>
</template>
