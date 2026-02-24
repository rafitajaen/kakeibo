<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useCategoriesStore } from "@/stores/categories";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import CategoryForm from "@/components/categories/CategoryForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const categoriesStore = useCategoriesStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    if (categoriesStore.categories.length === 0) {
        try {
            await categoriesStore.fetchCategories();
        } catch {
            apiError.value = t("categories.errors.unexpected");
        }
    }
});

const category = computed(() =>
    categoriesStore.categories.find((c) => c.id === (route.params.id as string)),
);

async function handleSubmit(values: { name: string }) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await categoriesStore.updateCategory(route.params.id as string, values);
        await router.push({ name: "categories" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            apiError.value = t("categories.errors.conflict");
        } else if (status === 403) {
            apiError.value = t("categories.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("categories.errors.notFound");
        } else {
            apiError.value = t("categories.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

            <Card v-if="category">
                <CardHeader>
                    <CardTitle>{{ t("categories.form.submitEdit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <CategoryForm
                        :initial-name="category.name"
                        :is-submitting="isSubmitting"
                        @submit="handleSubmit"
                    />
                </CardContent>
            </Card>

            <p v-else-if="!apiError" class="text-muted-foreground">{{ t("common.loading") }}</p>
        </div>
    </div>
</template>
