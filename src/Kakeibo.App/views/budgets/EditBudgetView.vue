<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useBudgetsStore } from "@/stores/budgets";
import type { UpdateBudgetData } from "@/stores/budgets";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { getHttpStatus } from "@/lib/http";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import BudgetForm from "@/components/budgets/BudgetForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const budgetsStore = useBudgetsStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([
            budgetsStore.budgets.length === 0 ? budgetsStore.fetchBudgets() : Promise.resolve(),
            categoriesStore.fetchCategories(),
            walletsStore.fetchWallets(),
        ]);
    } catch {
        apiError.value = t("wallets.budgets.errors.unexpected");
    }
});

const budget = computed(() =>
    budgetsStore.budgets.find((b) => b.id === (route.params.id as string)),
);

async function handleSubmit(values: UpdateBudgetData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await budgetsStore.updateBudget(route.params.id as string, values);
        await router.push({ name: "budgets" });
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 422) {
            apiError.value = t("wallets.budgets.errors.validation");
        } else if (status === 403) {
            apiError.value = t("wallets.budgets.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("wallets.budgets.errors.notFound");
        } else {
            apiError.value = t("wallets.budgets.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-md">
            <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

            <Card v-if="budget">
                <CardHeader>
                    <CardTitle>{{ t("wallets.budgets.form.submitEdit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <BudgetForm
                        :budget="budget"
                        :categories="categoriesStore.activeCategories"
                        :wallets="walletsStore.activeWallets"
                        :is-submitting="isSubmitting"
                        @submit="handleSubmit"
                    />
                </CardContent>
            </Card>

            <p v-else-if="!apiError" class="text-muted-foreground">{{ t("common.loading") }}</p>
        </div>
    </div>
</template>
