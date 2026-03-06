<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
import { useI18n } from "vue-i18n";
import type { Category } from "@/stores/categories";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";

defineProps<{
    // List of categories to display (should contain only active categories).
    categories: Category[];
    placeholder?: string;
}>();

// v-model compatible: emits 'update:modelValue' when selection changes.
const modelValue = defineModel<string>();

const { t } = useI18n();

// Resolves a lucide icon component by name, returns null if not found.
function getIcon(name: string | null | undefined) {
    if (!name) return null;
    return (LucideIcons as Record<string, unknown>)[name] ?? null;
}
</script>

<template>
    <Select v-model="modelValue">
        <SelectTrigger>
            <SelectValue :placeholder="placeholder ?? t('categories.form.name')" />
        </SelectTrigger>
        <SelectContent>
            <SelectItem v-for="category in categories" :key="category.id" :value="category.id">
                <div class="flex items-center gap-2">
                    <span
                        v-if="category.backgroundColor"
                        class="inline-flex items-center justify-center size-5 rounded-full shrink-0"
                        :style="{
                            backgroundColor: category.backgroundColor,
                            color: category.textColor ?? undefined,
                        }"
                    >
                        <component
                            v-if="getIcon(category.icon)"
                            :is="getIcon(category.icon)"
                            class="size-3"
                        />
                    </span>
                    <span>{{ category.name }}</span>
                </div>
            </SelectItem>
        </SelectContent>
    </Select>
</template>
