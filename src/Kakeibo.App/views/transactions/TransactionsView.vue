<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useTransactionsStore } from "@/stores/transactions";
import { useCategoriesStore } from "@/stores/categories";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import TransactionList from "@/components/transactions/TransactionList.vue";

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
    if (!confirm(t("transactions.deleteConfirm"))) return;
    try {
        await transactionsStore.deleteTransaction(id);
    } catch {
        apiError.value = t("transactions.errors.unexpected");
    }
}

function handleEdit(id: string) {
    router.push({ name: "transaction-edit", params: { walletId, id } });
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

        <TransactionList
            :transactions="transactionsStore.transactions"
            :is-loading="transactionsStore.isLoading"
            @edit="handleEdit"
            @delete="handleDelete"
        />
    </div>
</template>
