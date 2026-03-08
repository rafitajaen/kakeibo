import { ref, computed } from "vue";
import { startOfMonth, subMonths, subDays, eachDayOfInterval, format } from "date-fns";
import api from "@/lib/axios";
import type { Transaction } from "@/stores/transactions";

export interface DailyData {
    date: Date;
    income: number;
    expenses: number;
}

export interface MetricCard {
    key: string;
    label: string;
    value: number;
    trend: number;
    currency: string;
}

interface TransactionListResult {
    items: Transaction[];
    total: number;
}

// Fetches transactions for the last 90 days across multiple wallets and computes
// dashboard metrics (section cards) and daily chart data.
export function useDashboardData() {
    const allTransactions = ref<Transaction[]>([]);
    const isLoading = ref(false);

    // Fetch up to 500 transactions per wallet for the last 90 days.
    async function fetchAll(walletIds: string[]): Promise<void> {
        if (walletIds.length === 0) return;
        isLoading.value = true;
        try {
            const today = new Date();
            const fromDate = format(subDays(today, 90), "yyyy-MM-dd");
            const toDate = format(today, "yyyy-MM-dd");
            const results = await Promise.all(
                walletIds.map((walletId) =>
                    api
                        .get<TransactionListResult>("/api/transactions", {
                            params: { walletId, from: fromDate, to: toDate, pageSize: 500 },
                        })
                        .then((r) => r.data.items),
                ),
            );
            allTransactions.value = results.flat();
        } finally {
            isLoading.value = false;
        }
    }

    // Sums Income and Expense amounts for transactions matching the given date range.
    function sumPeriod(from: Date, to: Date): { income: number; expenses: number } {
        let income = 0;
        let expenses = 0;
        const fromStr = format(from, "yyyy-MM-dd");
        const toStr = format(to, "yyyy-MM-dd");
        for (const t of allTransactions.value) {
            if (t.date < fromStr || t.date > toStr) continue;
            if (t.type === "Income") income += t.amount;
            else if (t.type === "Expense") expenses += t.amount;
        }
        return { income, expenses };
    }

    // Aggregate metrics for section cards with month-over-month trend.
    const sectionMetrics = computed(() => {
        const today = new Date();
        const currentStart = startOfMonth(today);
        const previousStart = startOfMonth(subMonths(today, 1));
        const previousEnd = new Date(currentStart.getTime() - 1);

        const current = sumPeriod(currentStart, today);
        const previous = sumPeriod(previousStart, previousEnd);

        // Trend as percentage change; 0 when previous is 0.
        const incomeTrend =
            previous.income !== 0
                ? ((current.income - previous.income) / Math.abs(previous.income)) * 100
                : 0;
        const expensesTrend =
            previous.expenses !== 0
                ? ((current.expenses - previous.expenses) / Math.abs(previous.expenses)) * 100
                : 0;
        const netCurrent = current.income - current.expenses;
        const netPrevious = previous.income - previous.expenses;
        const netTrend =
            netPrevious !== 0 ? ((netCurrent - netPrevious) / Math.abs(netPrevious)) * 100 : 0;

        return {
            incomeThisMonth: current.income,
            expensesThisMonth: current.expenses,
            netSavings: netCurrent,
            incomeTrend,
            expensesTrend,
            netTrend,
        };
    });

    // Daily income/expense totals for the last 90 days (used by the area chart).
    // Pre-groups transactions by date string (yyyy-MM-dd) for O(1) lookup per day
    // instead of iterating all transactions for every day (O(days × transactions)).
    const chartData = computed<DailyData[]>(() => {
        const today = new Date();
        const start = subDays(today, 90);
        const days = eachDayOfInterval({ start, end: today });

        const byDate = new Map<string, { income: number; expenses: number }>();
        for (const t of allTransactions.value) {
            const key = t.date.slice(0, 10);
            const entry = byDate.get(key) ?? { income: 0, expenses: 0 };
            if (t.type === "Income") entry.income += t.amount;
            else if (t.type === "Expense") entry.expenses += t.amount;
            byDate.set(key, entry);
        }

        return days.map((day) => {
            const key = format(day, "yyyy-MM-dd");
            const { income, expenses } = byDate.get(key) ?? { income: 0, expenses: 0 };
            return { date: day, income, expenses };
        });
    });

    return {
        allTransactions,
        isLoading,
        fetchAll,
        sectionMetrics,
        chartData,
    };
}
