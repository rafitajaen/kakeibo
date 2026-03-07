import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Transaction {
    id: string;
    type: string; // "Income" | "Expense" | "Transfer"
    amount: number;
    description: string;
    date: string; // ISO date "YYYY-MM-DD"
    categoryId: string;
    categoryName: string;
    walletId: string;
    destinationWalletId?: string | null;
    notes?: string | null;
    createdAt: string;
}

export interface TransactionAttachment {
    id: string;
    transactionId: string;
    fileName: string;
    contentType: string;
    fileSizeBytes: number;
    uploadedByUserId: string;
    createdAt: string;
}

export interface RecordTransactionData {
    type: string;
    amount: number;
    description: string;
    date: string;
    categoryId: string;
    walletId: string;
    destinationWalletId?: string | null;
    notes?: string | null;
}

export interface UpdateTransactionData {
    amount: number;
    description: string;
    date: string;
    categoryId: string;
    destinationWalletId?: string | null;
    notes?: string | null;
}

export interface ListTransactionsParams {
    walletId: string;
    page?: number;
    pageSize?: number;
    from?: string | null;
    to?: string | null;
    categoryId?: string | null;
    type?: string | null;
}

export interface TransactionListResult {
    items: Transaction[];
    total: number;
}

export const useTransactionsStore = defineStore("transactions", () => {
    // State
    const transactions = ref<Transaction[]>([]);
    const currentTransaction = ref<Transaction | null>(null);
    const total = ref(0);
    const isLoading = ref(false);
    const attachments = ref<TransactionAttachment[]>([]);
    const isAttachmentsLoading = ref(false);

    // Getters
    const incomeTransactions = computed(() =>
        transactions.value.filter((t) => t.type === "Income"),
    );
    const expenseTransactions = computed(() =>
        transactions.value.filter((t) => t.type === "Expense"),
    );

    // Fetches a paginated, filtered list of transactions for a wallet.
    async function fetchTransactions(params: ListTransactionsParams): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<TransactionListResult>("/api/transactions", {
                params: {
                    walletId: params.walletId,
                    page: params.page ?? 1,
                    pageSize: params.pageSize ?? 50,
                    ...(params.from ? { from: params.from } : {}),
                    ...(params.to ? { to: params.to } : {}),
                    ...(params.categoryId ? { categoryId: params.categoryId } : {}),
                    ...(params.type ? { type: params.type } : {}),
                },
            });
            transactions.value = response.data.items;
            total.value = response.data.total;
        } finally {
            isLoading.value = false;
        }
    }

    // Fetches a single transaction and sets it as the current transaction.
    async function fetchTransaction(id: string): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<Transaction>(`/api/transactions/${id}`);
            currentTransaction.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Records a new transaction and returns the created resource.
    async function recordTransaction(data: RecordTransactionData): Promise<Transaction> {
        const response = await api.post<Transaction>("/api/transactions", data);
        return response.data;
    }

    // Updates an existing transaction and refreshes the local list entry.
    async function updateTransaction(
        id: string,
        data: UpdateTransactionData,
    ): Promise<Transaction> {
        const response = await api.put<Transaction>(`/api/transactions/${id}`, data);
        const index = transactions.value.findIndex((t) => t.id === id);
        if (index !== -1) {
            transactions.value[index] = response.data;
        }
        if (currentTransaction.value?.id === id) {
            currentTransaction.value = response.data;
        }
        return response.data;
    }

    // Soft-deletes a transaction and removes it from the local list.
    async function deleteTransaction(id: string): Promise<void> {
        await api.delete(`/api/transactions/${id}`);
        transactions.value = transactions.value.filter((t) => t.id !== id);
        if (currentTransaction.value?.id === id) {
            currentTransaction.value = null;
        }
        total.value = Math.max(0, total.value - 1);
    }

    // Fetches all attachments for a transaction.
    async function fetchAttachments(transactionId: string): Promise<void> {
        isAttachmentsLoading.value = true;
        try {
            const response = await api.get<{ items: TransactionAttachment[] }>(
                `/api/transactions/${transactionId}/attachments`,
            );
            attachments.value = response.data.items;
        } finally {
            isAttachmentsLoading.value = false;
        }
    }

    // Uploads a file attachment for a transaction and appends it to local state.
    async function uploadAttachment(
        transactionId: string,
        file: File,
    ): Promise<TransactionAttachment> {
        const formData = new FormData();
        formData.append("file", file);
        const response = await api.post<TransactionAttachment>(
            `/api/transactions/${transactionId}/attachments`,
            formData,
            { headers: { "Content-Type": "multipart/form-data" } },
        );
        attachments.value.push(response.data);
        return response.data;
    }

    // Downloads an attachment by triggering a browser download via an object URL.
    async function downloadAttachment(
        transactionId: string,
        attachmentId: string,
        fileName: string,
    ): Promise<void> {
        const response = await api.get(
            `/api/transactions/${transactionId}/attachments/${attachmentId}`,
            { responseType: "blob" },
        );
        const url = URL.createObjectURL(response.data as Blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
    }

    // Deletes an attachment and removes it from local state.
    async function deleteAttachment(transactionId: string, attachmentId: string): Promise<void> {
        await api.delete(`/api/transactions/${transactionId}/attachments/${attachmentId}`);
        attachments.value = attachments.value.filter((a) => a.id !== attachmentId);
    }

    // Fetches recent transactions from multiple wallets for the dashboard.
    // Returns merged list sorted by date descending without mutating store state.
    async function fetchRecentForDashboard(
        walletIds: string[],
        limit = 10,
    ): Promise<Transaction[]> {
        if (walletIds.length === 0) return [];
        const results = await Promise.all(
            walletIds.map((walletId) =>
                api
                    .get<TransactionListResult>("/api/transactions", {
                        params: { walletId, page: 1, pageSize: limit },
                    })
                    .then((r) => r.data.items),
            ),
        );
        return results
            .flat()
            .sort((a, b) => b.date.localeCompare(a.date))
            .slice(0, limit);
    }

    return {
        transactions,
        currentTransaction,
        total,
        isLoading,
        attachments,
        isAttachmentsLoading,
        incomeTransactions,
        expenseTransactions,
        fetchTransactions,
        fetchTransaction,
        recordTransaction,
        updateTransaction,
        deleteTransaction,
        fetchRecentForDashboard,
        fetchAttachments,
        uploadAttachment,
        downloadAttachment,
        deleteAttachment,
    };
});
