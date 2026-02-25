<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useWalletsStore } from "@/stores/wallets";
import { useTransactionsStore } from "@/stores/transactions";
import { useBudgetsStore } from "@/stores/budgets";
import { useGoalsStore } from "@/stores/goals";
import type { Transaction } from "@/stores/transactions";
import BalanceOverview from "@/components/dashboard/BalanceOverview.vue";
import RecentTransactions from "@/components/dashboard/RecentTransactions.vue";
import BudgetSummary from "@/components/dashboard/BudgetSummary.vue";
import GoalSummary from "@/components/dashboard/GoalSummary.vue";
import QuickActions from "@/components/dashboard/QuickActions.vue";

const { t } = useI18n();
const walletsStore = useWalletsStore();
const transactionsStore = useTransactionsStore();
const budgetsStore = useBudgetsStore();
const goalsStore = useGoalsStore();

const recentTransactions = ref<Transaction[]>([]);
const isLoading = ref(true);
const isLoadingTransactions = ref(true);

onMounted(async () => {
    isLoading.value = true;
    isLoadingTransactions.value = true;
    try {
        await Promise.all([
            walletsStore.fetchWallets(),
            budgetsStore.fetchBudgets(),
            goalsStore.fetchGoals(),
        ]);
        const walletIds = walletsStore.activeWallets.map((w) => w.id);
        recentTransactions.value = await transactionsStore.fetchRecentForDashboard(walletIds);
    } finally {
        isLoading.value = false;
        isLoadingTransactions.value = false;
    }
});
</script>

<template>
    <div class="container mx-auto px-4 py-8">
        <h1 class="text-2xl font-bold mb-6">{{ t("dashboard.title") }}</h1>

        <div class="grid gap-6">
            <!-- Balance Overview — full width -->
            <BalanceOverview :wallets="walletsStore.activeWallets" :is-loading="isLoading" />

            <!-- Budgets + Goals — side by side on medium screens -->
            <div class="grid md:grid-cols-2 gap-6">
                <BudgetSummary :budgets="budgetsStore.budgets" :is-loading="isLoading" />
                <GoalSummary :goals="goalsStore.goals" :is-loading="isLoading" />
            </div>

            <!-- Recent Transactions + Quick Actions -->
            <div class="grid md:grid-cols-2 gap-6">
                <RecentTransactions
                    :transactions="recentTransactions"
                    :is-loading="isLoadingTransactions"
                />
                <QuickActions :wallets="walletsStore.activeWallets" />
            </div>
        </div>
    </div>
</template>
