<script setup lang="ts">
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
</script>

<template>
    <Select v-model="modelValue">
        <SelectTrigger>
            <SelectValue :placeholder="placeholder ?? t('categories.form.name')" />
        </SelectTrigger>
        <SelectContent>
            <SelectItem v-for="category in categories" :key="category.id" :value="category.id">
                {{ category.name }}
            </SelectItem>
        </SelectContent>
    </Select>
</template>
