<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useRecurringStore } from "@/stores/recurring";
import type { CreatePatternData, UpdatePatternData } from "@/stores/recurring";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { getHttpStatus } from "@/lib/http";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import RecurringForm from "@/components/recurring/RecurringForm.vue";

const { t } = useI18n();
const router = useRouter();
const recurringStore = useRecurringStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([categoriesStore.fetchCategories(), walletsStore.fetchWallets()]);
    } catch {
        apiError.value = t("wallets.recurring.errors.unexpected");
    }
});

async function handleSubmit(values: CreatePatternData | UpdatePatternData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await recurringStore.createPattern(values as CreatePatternData);
        await router.push({ name: "recurring" });
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 422) {
            apiError.value = t("wallets.recurring.errors.validation");
        } else {
            apiError.value = t("wallets.recurring.errors.unexpected");
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
                    <CardTitle>{{ t("wallets.recurring.form.submit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <RecurringForm
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
