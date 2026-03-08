<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useBudgetsStore } from "@/stores/budgets";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { getHttpStatus } from "@/lib/http";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import BudgetList from "@/components/budgets/BudgetList.vue";

const { t } = useI18n();
const router = useRouter();
const budgetsStore = useBudgetsStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);

onMounted(async () => {
    try {
        await Promise.all([
            budgetsStore.fetchBudgets(),
            categoriesStore.fetchCategories(),
            walletsStore.fetchWallets(),
        ]);
    } catch {
        apiError.value = t("wallets.budgets.errors.unexpected");
    }
});

async function handleDelete(id: string) {
    if (!confirm(t("wallets.budgets.actions.deleteConfirm"))) return;
    try {
        await budgetsStore.deleteBudget(id);
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 403) {
            apiError.value = t("wallets.budgets.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("wallets.budgets.errors.notFound");
        } else {
            apiError.value = t("wallets.budgets.errors.unexpected");
        }
    }
}

function handleEdit(id: string) {
    router.push({ name: "budget-edit", params: { id } });
}
</script>

<template>
    <div class="container mx-auto max-w-2xl px-4 py-8">
        <div class="mb-6 flex items-center justify-between">
            <h1 class="text-2xl font-bold">{{ t("wallets.budgets.title") }}</h1>
            <Button @click="router.push({ name: 'budget-create' })">
                {{ t("wallets.budgets.createNew") }}
            </Button>
        </div>

        <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

        <Card>
            <CardContent class="pt-6">
                <BudgetList
                    :budgets="budgetsStore.budgets"
                    :is-loading="budgetsStore.isLoading"
                    @edit="handleEdit"
                    @delete="handleDelete"
                />
            </CardContent>
        </Card>
    </div>
</template>
