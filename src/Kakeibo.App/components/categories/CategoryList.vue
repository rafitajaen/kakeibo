<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
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

// Resolves a lucide icon component by name, returns null if not found.
function getIcon(name: string | null | undefined) {
    if (!name) return null;
    return (LucideIcons as Record<string, unknown>)[name] ?? null;
}
</script>

<template>
    <div>
        <p v-if="categories.length === 0" class="px-4 py-6 text-sm text-muted-foreground">
            {{ t("categories.empty") }}
        </p>

        <div class="divide-y divide-border">
            <div
                v-for="category in categories"
                :key="category.id"
                class="flex items-center gap-3 px-4 py-3"
            >
                <!-- Category icon circle -->
                <div
                    class="flex size-10 shrink-0 items-center justify-center rounded-full"
                    :style="
                        category.backgroundColor
                            ? { backgroundColor: category.backgroundColor }
                            : { backgroundColor: 'oklch(0.556 0.127 177.5)' }
                    "
                >
                    <component
                        v-if="getIcon(category.icon)"
                        :is="getIcon(category.icon)"
                        class="size-4 text-white"
                    />
                </div>

                <div class="flex min-w-0 flex-1 items-center gap-2">
                    <span class="truncate text-sm font-medium">{{ category.name }}</span>
                    <Badge v-if="category.isArchived" variant="secondary" class="text-xs shrink-0">
                        {{ t("wallets.archived") }}
                    </Badge>
                </div>

                <!-- Action buttons only for custom, non-archived categories. -->
                <div
                    v-if="showActions && !category.isSystem && !category.isArchived"
                    class="flex shrink-0 gap-1"
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
    </div>
</template>
