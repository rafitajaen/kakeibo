<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useCategoriesStore } from "@/stores/categories";
import { getHttpStatus } from "@/lib/http";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import CategoryForm from "@/components/categories/CategoryForm.vue";

const { t } = useI18n();
const router = useRouter();
const categoriesStore = useCategoriesStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

async function handleSubmit(values: {
    name: string;
    icon: string | null;
    backgroundColor: string | null;
    textColor: string | null;
    isPrivate: boolean;
}) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await categoriesStore.createCategory(values);
        await router.push({ name: "categories" });
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 409) {
            apiError.value = t("categories.errors.conflict");
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
            <Card>
                <CardHeader>
                    <CardTitle>{{ t("categories.form.submit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <CategoryForm :is-submitting="isSubmitting" @submit="handleSubmit" />
                    <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
