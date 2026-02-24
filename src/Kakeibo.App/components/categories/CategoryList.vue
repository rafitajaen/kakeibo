<script setup lang="ts">
import { useI18n } from "vue-i18n";
import type { Category } from "@/stores/categories";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

defineProps<{
    categories: Category[];
    // Controls visibility of edit/archive buttons (hidden for system categories).
    showActions?: boolean;
}>();

const emit = defineEmits<{
    (e: "edit", category: Category): void;
    (e: "archive", category: Category): void;
}>();

const { t } = useI18n();
</script>

<template>
    <div class="space-y-2">
        <p v-if="categories.length === 0" class="text-sm text-muted-foreground">
            {{ t("categories.empty") }}
        </p>

        <div
            v-for="category in categories"
            :key="category.id"
            class="flex items-center justify-between rounded-md border px-4 py-2"
        >
            <div class="flex items-center gap-2">
                <span class="text-sm font-medium">{{ category.name }}</span>
                <Badge v-if="category.isArchived" variant="secondary" class="text-xs">
                    {{ t("wallets.archived") }}
                </Badge>
            </div>

            <!-- Action buttons only for custom, non-archived categories. -->
            <div
                v-if="showActions && !category.isSystem && !category.isArchived"
                class="flex gap-1"
            >
                <Button variant="ghost" size="sm" @click="emit('edit', category)">
                    {{ t("categories.actions.edit") }}
                </Button>
                <Button
                    variant="ghost"
                    size="sm"
                    class="text-destructive hover:text-destructive"
                    @click="emit('archive', category)"
                >
                    {{ t("categories.actions.archive") }}
                </Button>
            </div>
        </div>
    </div>
</template>
