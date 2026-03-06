<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
import { Wallet } from "lucide-vue-next";

const props = defineProps<{
    backgroundColor: string;
    textColor: string;
    iconName?: string | null;
    walletName?: string;
}>();

const emit = defineEmits<{
    (e: "update:backgroundColor", value: string): void;
    (e: "update:textColor", value: string): void;
}>();

// Tailwind 500-level palette + neutrals
const BG_COLORS = [
    { hex: "#EF4444", label: "Red" },
    { hex: "#F97316", label: "Orange" },
    { hex: "#F59E0B", label: "Amber" },
    { hex: "#EAB308", label: "Yellow" },
    { hex: "#84CC16", label: "Lime" },
    { hex: "#22C55E", label: "Green" },
    { hex: "#10B981", label: "Emerald" },
    { hex: "#14B8A6", label: "Teal" },
    { hex: "#06B6D4", label: "Cyan" },
    { hex: "#0EA5E9", label: "Sky" },
    { hex: "#3B82F6", label: "Blue" },
    { hex: "#6366F1", label: "Indigo" },
    { hex: "#8B5CF6", label: "Violet" },
    { hex: "#A855F7", label: "Purple" },
    { hex: "#D946EF", label: "Fuchsia" },
    { hex: "#EC4899", label: "Pink" },
    { hex: "#F43F5E", label: "Rose" },
    { hex: "#64748B", label: "Slate" },
    { hex: "#6B7280", label: "Gray" },
    { hex: "#1F2937", label: "Dark" },
    { hex: "#FFFFFF", label: "White" },
    { hex: "#F3F4F6", label: "Light" },
    { hex: "#4B5563", label: "Neutral" },
    { hex: "#111827", label: "Black" },
];

const TEXT_COLORS = [
    { hex: "#FFFFFF", label: "White" },
    { hex: "#111827", label: "Black" },
    { hex: "#6B7280", label: "Gray" },
    { hex: "#1F2937", label: "Dark gray" },
    { hex: "#FEF3C7", label: "Pale yellow" },
];

function getIcon() {
    if (!props.iconName) return Wallet;
    return (LucideIcons as Record<string, unknown>)[props.iconName] ?? Wallet;
}
</script>

<template>
    <div class="space-y-4">
        <!-- Live preview -->
        <div
            class="flex items-center gap-3 rounded-lg p-4 border"
            :style="{ backgroundColor: backgroundColor, color: textColor }"
        >
            <component :is="getIcon()" class="size-5 shrink-0" />
            <span class="font-medium truncate">{{ walletName || "My Wallet" }}</span>
        </div>

        <!-- Background color -->
        <div>
            <p class="text-sm font-medium mb-2">Background</p>
            <div class="grid grid-cols-8 gap-1">
                <button
                    v-for="color in BG_COLORS"
                    :key="color.hex"
                    type="button"
                    :title="color.label"
                    class="size-7 rounded border-2 transition-all"
                    :style="{ backgroundColor: color.hex }"
                    :class="
                        backgroundColor === color.hex
                            ? 'border-primary scale-110'
                            : 'border-transparent'
                    "
                    @click="emit('update:backgroundColor', color.hex)"
                />
            </div>
        </div>

        <!-- Text color -->
        <div>
            <p class="text-sm font-medium mb-2">Text</p>
            <div class="flex gap-1">
                <button
                    v-for="color in TEXT_COLORS"
                    :key="color.hex"
                    type="button"
                    :title="color.label"
                    class="size-7 rounded border-2 transition-all"
                    :style="{ backgroundColor: color.hex }"
                    :class="
                        textColor === color.hex
                            ? 'border-primary scale-110'
                            : 'border-muted-foreground/30'
                    "
                    @click="emit('update:textColor', color.hex)"
                />
            </div>
        </div>
    </div>
</template>
