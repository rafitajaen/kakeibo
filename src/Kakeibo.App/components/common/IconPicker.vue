<script setup lang="ts">
import { ref, computed } from "vue";
import * as LucideIcons from "lucide-vue-next";
import { Search } from "lucide-vue-next";
import { Input } from "@/components/ui/input";
import { ICON_GROUPS, ALL_ICONS } from "@/lib/icon-catalog";

const props = defineProps<{
    modelValue: string | null;
}>();

const emit = defineEmits<{
    (e: "update:modelValue", value: string | null): void;
}>();

const search = ref("");

const filteredGroups = computed(() => {
    const q = search.value.trim().toLowerCase();
    if (!q) return ICON_GROUPS;
    const matches = ALL_ICONS.filter(
        (i) => i.label.toLowerCase().includes(q) || i.name.toLowerCase().includes(q),
    );
    if (!matches.length) return [];
    return [{ group: "Results", icons: matches }];
});

function getIcon(name: string) {
    return (LucideIcons as Record<string, unknown>)[name] ?? LucideIcons.Wallet;
}

function select(name: string) {
    emit("update:modelValue", props.modelValue === name ? null : name);
}
</script>

<template>
    <div class="space-y-3">
        <div class="relative">
            <Search class="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input v-model="search" placeholder="Search icons..." class="pl-8" />
        </div>
        <div class="h-64 overflow-y-auto space-y-4 pr-1">
            <div v-for="group in filteredGroups" :key="group.group">
                <p class="text-xs font-medium text-muted-foreground mb-1.5">{{ group.group }}</p>
                <div class="grid grid-cols-8 gap-1">
                    <button
                        v-for="icon in group.icons"
                        :key="icon.name"
                        :title="icon.label"
                        type="button"
                        class="flex items-center justify-center size-8 rounded border transition-colors"
                        :class="
                            modelValue === icon.name
                                ? 'bg-primary text-primary-foreground border-primary'
                                : 'border-transparent hover:bg-muted'
                        "
                        @click="select(icon.name)"
                    >
                        <component :is="getIcon(icon.name)" class="size-4" />
                    </button>
                </div>
            </div>
            <p v-if="!filteredGroups.length" class="text-sm text-muted-foreground text-center py-4">
                No icons found
            </p>
        </div>
    </div>
</template>
