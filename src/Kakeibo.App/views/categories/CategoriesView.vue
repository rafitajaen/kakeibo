<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useCategoriesStore } from "@/stores/categories";
import type { Category } from "@/stores/categories";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import CategoryList from "@/components/categories/CategoryList.vue";

const { t } = useI18n();
const router = useRouter();
const categoriesStore = useCategoriesStore();

const apiError = ref<string | null>(null);

onMounted(async () => {
    try {
        await categoriesStore.fetchCategories(true);
    } catch {
        apiError.value = t("categories.errors.unexpected");
    }
});

const systemCategories = computed(() => categoriesStore.systemCategories);
const customCategories = computed(() => categoriesStore.customCategories);

async function handleArchive(category: Category) {
    if (!confirm(t("categories.actions.archiveConfirm"))) return;
    try {
        await categoriesStore.archiveCategory(category.id);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            apiError.value = t("categories.errors.hasTransactions");
        } else if (status === 403) {
            apiError.value = t("categories.errors.forbidden");
        } else {
            apiError.value = t("categories.errors.unexpected");
        }
    }
}

function handleEdit(category: Category) {
    router.push({ name: "category-edit", params: { id: category.id } });
}
</script>

<template>
    <div class="container mx-auto max-w-2xl px-4 py-8">
        <div class="mb-6 flex items-center justify-between">
            <h1 class="text-2xl font-bold">{{ t("categories.title") }}</h1>
            <Button @click="router.push({ name: 'category-create' })">
                {{ t("categories.createNew") }}
            </Button>
        </div>

        <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

        <Card>
            <CardContent class="pt-6">
                <Tabs default-value="system">
                    <TabsList class="mb-4">
                        <TabsTrigger value="system">{{ t("categories.system") }}</TabsTrigger>
                        <TabsTrigger value="custom">{{ t("categories.custom") }}</TabsTrigger>
                    </TabsList>

                    <TabsContent value="system">
                        <CategoryList :categories="systemCategories" :show-actions="false" />
                    </TabsContent>

                    <TabsContent value="custom">
                        <CategoryList
                            :categories="customCategories"
                            :show-actions="true"
                            @edit="handleEdit"
                            @archive="handleArchive"
                        />
                    </TabsContent>
                </Tabs>
            </CardContent>
        </Card>
    </div>
</template>
