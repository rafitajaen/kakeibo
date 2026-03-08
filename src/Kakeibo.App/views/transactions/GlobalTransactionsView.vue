<script setup lang="ts">
import { ref, watch, onMounted } from "vue";
import { useRouter } from "vue-router";
import { format, startOfMonth, endOfMonth } from "date-fns";
import { useTransactionsStore } from "@/stores/transactions";
import { useWalletsStore } from "@/stores/wallets";
import PeriodSelector, { type PeriodValue } from "@/components/PeriodSelector.vue";
import TransactionList from "@/components/transactions/TransactionList.vue";

const router = useRouter();
const transactionsStore = useTransactionsStore();
const walletsStore = useWalletsStore();

const now = new Date();
const period = ref<PeriodValue>({
    type: "month",
    anchor: format(now, "yyyy-MM-dd"),
    from: format(startOfMonth(now), "yyyy-MM-dd"),
    to: format(endOfMonth(now), "yyyy-MM-dd"),
});

async function load() {
    await transactionsStore.fetchAllTransactions({
        from: period.value.from ?? undefined,
        to: period.value.to ?? undefined,
    });
}

onMounted(async () => {
    if (walletsStore.wallets.length === 0) await walletsStore.fetchWallets(true);
    await load();
});

watch(period, () => load(), { deep: true });

function handleEdit(id: string) {
    // Find the transaction to get its walletId for the edit route
    const tx = transactionsStore.allTransactions.find((t) => t.id === id);
    if (tx) {
        router.push({ name: "transaction-edit", params: { walletId: tx.walletId, id } });
    }
}
</script>

<template>
    <div class="flex flex-col min-h-full">
        <PeriodSelector v-model="period" />

        <TransactionList
            :transactions="transactionsStore.allTransactions"
            :is-loading="transactionsStore.isAllLoading"
            @edit="handleEdit"
        />
    </div>
</template>
