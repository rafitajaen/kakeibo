<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useTransactionsStore } from "@/stores/transactions";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import TransactionForm from "@/components/transactions/TransactionForm.vue";
import TransactionAttachmentList from "@/components/transactions/TransactionAttachmentList.vue";
import type { TransactionAttachment } from "@/stores/transactions";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const transactionsStore = useTransactionsStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const walletId = route.params.walletId as string;
const transactionId = route.params.id as string;

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([
            transactionsStore.fetchTransaction(transactionId),
            transactionsStore.fetchAttachments(transactionId),
            categoriesStore.fetchCategories(),
            walletsStore.fetchWallets(),
        ]);
    } catch {
        apiError.value = t("transactions.errors.notFound");
    }
});

async function handleUploadAttachment(file: File): Promise<void> {
    try {
        await transactionsStore.uploadAttachment(transactionId, file);
    } catch {
        apiError.value = t("transactions.attachments.uploadError");
    }
}

async function handleDownloadAttachment(attachment: TransactionAttachment): Promise<void> {
    try {
        await transactionsStore.downloadAttachment(
            transactionId,
            attachment.id,
            attachment.fileName,
        );
    } catch {
        apiError.value = t("transactions.attachments.downloadError");
    }
}

async function handleDeleteAttachment(attachmentId: string): Promise<void> {
    if (!confirm(t("transactions.attachments.deleteConfirm"))) return;
    try {
        await transactionsStore.deleteAttachment(transactionId, attachmentId);
    } catch {
        apiError.value = t("transactions.attachments.deleteError");
    }
}

async function handleSubmit(values: {
    amount: number;
    description: string;
    date: string;
    categoryId: string;
    destinationWalletId?: string | null;
    notes?: string | null;
}) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await transactionsStore.updateTransaction(transactionId, values);
        await router.push({ name: "transactions", params: { walletId } });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
            apiError.value = t("transactions.errors.notFound");
        } else if (status === 403) {
            apiError.value = t("transactions.errors.forbidden");
        } else {
            apiError.value = t("transactions.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <Card>
                <CardHeader>
                    <CardTitle>{{ t("transactions.form.submitEdit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <template v-if="transactionsStore.currentTransaction">
                        <TransactionForm
                            :initial-data="{
                                type: transactionsStore.currentTransaction.type,
                                amount: transactionsStore.currentTransaction.amount,
                                description: transactionsStore.currentTransaction.description,
                                date: transactionsStore.currentTransaction.date,
                                categoryId: transactionsStore.currentTransaction.categoryId,
                                destinationWalletId:
                                    transactionsStore.currentTransaction.destinationWalletId,
                                notes: transactionsStore.currentTransaction.notes,
                            }"
                            :is-submitting="isSubmitting"
                            :categories="categoriesStore.activeCategories"
                            :wallets="walletsStore.wallets"
                            :source-wallet-id="walletId"
                            @submit="handleSubmit"
                        />
                        <div class="mt-4">
                            <TransactionAttachmentList
                                :transaction-id="transactionId"
                                :attachments="transactionsStore.attachments"
                                :is-loading="transactionsStore.isAttachmentsLoading"
                                @upload="handleUploadAttachment"
                                @download="handleDownloadAttachment"
                                @delete="handleDeleteAttachment"
                            />
                        </div>
                        <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                    </template>
                    <p v-else class="text-muted-foreground">{{ t("common.loading") }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
