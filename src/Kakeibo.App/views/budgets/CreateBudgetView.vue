<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useBudgetsStore } from "@/stores/budgets";
import type { CreateBudgetData } from "@/stores/budgets";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import BudgetForm from "@/components/budgets/BudgetForm.vue";

const { t } = useI18n();
const router = useRouter();
const budgetsStore = useBudgetsStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([categoriesStore.fetchCategories(), walletsStore.fetchWallets()]);
    } catch {
        apiError.value = t("wallets.budgets.errors.unexpected");
    }
});

async function handleSubmit(values: CreateBudgetData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await budgetsStore.createBudget(values);
        await router.push({ name: "budgets" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 422) {
            apiError.value = t("wallets.budgets.errors.validation");
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
            <Card>
                <CardHeader>
                    <CardTitle>{{ t("wallets.budgets.form.submit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <BudgetForm
                        :categories="categoriesStore.activeCategories"
                        :wallets="walletsStore.activeWallets"
                        :is-submitting="isSubmitting"
                        @submit="handleSubmit"
                    />
                    <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
