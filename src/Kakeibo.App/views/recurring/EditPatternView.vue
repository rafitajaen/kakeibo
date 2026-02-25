<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useRecurringStore } from "@/stores/recurring";
import type { UpdatePatternData } from "@/stores/recurring";
import { useCategoriesStore } from "@/stores/categories";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import RecurringForm from "@/components/recurring/RecurringForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const recurringStore = useRecurringStore();
const categoriesStore = useCategoriesStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([
            recurringStore.patterns.length === 0
                ? recurringStore.fetchPatterns()
                : Promise.resolve(),
            categoriesStore.fetchCategories(),
            walletsStore.fetchWallets(),
        ]);
    } catch {
        apiError.value = t("wallets.recurring.errors.unexpected");
    }
});

const pattern = computed(() =>
    recurringStore.patterns.find((p) => p.id === (route.params.id as string)),
);

async function handleSubmit(values: UpdatePatternData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await recurringStore.updatePattern(route.params.id as string, values);
        await router.push({ name: "recurring" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 422) {
            apiError.value = t("wallets.recurring.errors.validation");
        } else if (status === 403) {
            apiError.value = t("wallets.recurring.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("wallets.recurring.errors.notFound");
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
            <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

            <Card v-if="pattern">
                <CardHeader>
                    <CardTitle>{{ t("wallets.recurring.form.submitEdit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <RecurringForm
                        :pattern="pattern"
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
