<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { useTransactionsStore } from "@/stores/transactions";
import { useBudgetsStore } from "@/stores/budgets";
import { useGoalsStore } from "@/stores/goals";
import { useDashboardData } from "@/composables/useDashboardData";
import type { Transaction } from "@/stores/transactions";
import SectionCards from "@/components/dashboard/SectionCards.vue";
import ChartAreaInteractive from "@/components/dashboard/ChartAreaInteractive.vue";
import RecentTransactions from "@/components/dashboard/RecentTransactions.vue";
import BudgetSummary from "@/components/dashboard/BudgetSummary.vue";
import GoalSummary from "@/components/dashboard/GoalSummary.vue";

const router = useRouter();
const walletsStore = useWalletsStore();
const transactionsStore = useTransactionsStore();
const budgetsStore = useBudgetsStore();
const goalsStore = useGoalsStore();
const { sectionMetrics, chartData, fetchAll, isLoading: isDashboardLoading } = useDashboardData();

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
        // Redirect new users (no wallets) to onboarding wizard.
        if (walletsStore.activeWallets.length === 0) {
            router.replace({ name: "onboarding" });
            return;
        }
        const walletIds = walletsStore.activeWallets.map((w) => w.id);
        await Promise.all([
            fetchAll(walletIds),
            transactionsStore
                .fetchRecentForDashboard(walletIds)
                .then((t) => (recentTransactions.value = t)),
        ]);
    } finally {
        isLoading.value = false;
        isLoadingTransactions.value = false;
    }
});
</script>

<template>
    <div class="flex flex-col gap-4 py-4 md:gap-6 md:py-6">
        <!-- Metric cards -->
        <SectionCards :metrics="sectionMetrics" :is-loading="isLoading || isDashboardLoading" />

        <!-- Area chart -->
        <div class="px-4 lg:px-6">
            <ChartAreaInteractive :data="chartData" />
        </div>

        <!-- Budgets + Goals -->
        <div class="grid gap-4 px-4 md:grid-cols-2 lg:px-6">
            <BudgetSummary :budgets="budgetsStore.budgets" :is-loading="isLoading" />
            <GoalSummary :goals="goalsStore.goals" :is-loading="isLoading" />
        </div>

        <!-- Recent Transactions -->
        <div class="px-4 lg:px-6">
            <RecentTransactions
                :transactions="recentTransactions"
                :is-loading="isLoadingTransactions"
            />
        </div>
    </div>
</template>
