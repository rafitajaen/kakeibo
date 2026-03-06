<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import * as LucideIcons from "lucide-vue-next";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";

// Curated list of ~30 lucide icons for category customization.
const ICON_LIST = [
    "Home",
    "Car",
    "UtensilsCrossed",
    "Heart",
    "Tv",
    "ShoppingCart",
    "BookOpen",
    "Repeat",
    "PiggyBank",
    "CreditCard",
    "Gift",
    "MoreHorizontal",
    "Coffee",
    "Plane",
    "Music",
    "Camera",
    "Dumbbell",
    "Shirt",
    "Laptop",
    "Phone",
    "Fuel",
    "Bus",
    "Star",
    "Wallet",
    "TrendingUp",
    "Building",
    "Briefcase",
    "Baby",
    "Gamepad2",
    "Scissors",
];

// Preset background color palette.
const BG_COLORS = [
    { value: "#EFF6FF", label: "Blue" },
    { value: "#F0FDF4", label: "Green" },
    { value: "#FFF7ED", label: "Orange" },
    { value: "#FFF1F2", label: "Red" },
    { value: "#FAF5FF", label: "Purple" },
    { value: "#FDF2F8", label: "Pink" },
    { value: "#EEF2FF", label: "Indigo" },
    { value: "#F0FDFA", label: "Teal" },
    { value: "#ECFDF5", label: "Emerald" },
    { value: "#FEFCE8", label: "Yellow" },
    { value: "#F9FAFB", label: "Gray" },
    { value: "#FFFFFF", label: "White" },
];

// Preset text color palette.
const TEXT_COLORS = [
    { value: "#1D4ED8", label: "Blue" },
    { value: "#15803D", label: "Green" },
    { value: "#C2410C", label: "Orange" },
    { value: "#BE123C", label: "Red" },
    { value: "#7E22CE", label: "Purple" },
    { value: "#9D174D", label: "Pink" },
    { value: "#3730A3", label: "Indigo" },
    { value: "#0F766E", label: "Teal" },
    { value: "#374151", label: "Gray" },
    { value: "#111827", label: "Black" },
];

const props = defineProps<{
    initialName?: string;
    initialIcon?: string | null;
    initialBackgroundColor?: string | null;
    initialTextColor?: string | null;
    isSystem?: boolean;
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (
        e: "submit",
        values: {
            name: string;
            icon: string | null;
            backgroundColor: string | null;
            textColor: string | null;
        },
    ): void;
}>();

const { t } = useI18n();

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(50),
        icon: z.string().nullable().optional(),
        backgroundColor: z.string().nullable().optional(),
        textColor: z.string().nullable().optional(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        name: props.initialName ?? "",
        icon: props.initialIcon ?? null,
        backgroundColor: props.initialBackgroundColor ?? null,
        textColor: props.initialTextColor ?? null,
    },
});

// Resolves a lucide icon component by name.
function getIcon(name: string | null | undefined) {
    if (!name) return null;
    return (LucideIcons as Record<string, unknown>)[name] ?? null;
}

// Live badge preview using current form values.
const previewBg = computed(() => form.values.backgroundColor ?? "#F9FAFB");
const previewText = computed(() => form.values.textColor ?? "#374151");
const previewIcon = computed(() => getIcon(form.values.icon));
const previewName = computed(() => form.values.name || t("categories.form.previewPlaceholder"));

const onSubmit = form.handleSubmit((values) => {
    emit("submit", {
        name: values.name,
        icon: values.icon ?? null,
        backgroundColor: values.backgroundColor ?? null,
        textColor: values.textColor ?? null,
    });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Name field (always visible) -->
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("categories.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('categories.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Color & icon customization — only for custom categories -->
        <template v-if="!isSystem">
            <!-- Icon selector -->
            <FormField v-slot="{ componentField }" name="icon">
                <FormItem>
                    <FormLabel>{{ t("categories.form.icon") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue :placeholder="t('categories.form.iconPlaceholder')" />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent class="max-h-60">
                            <SelectItem
                                v-for="iconName in ICON_LIST"
                                :key="iconName"
                                :value="iconName"
                            >
                                <div class="flex items-center gap-2">
                                    <component
                                        :is="(LucideIcons as Record<string, unknown>)[iconName]"
                                        class="size-4 shrink-0"
                                    />
                                    <span>{{ iconName }}</span>
                                </div>
                            </SelectItem>
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <!-- Background color picker -->
            <FormField name="backgroundColor" v-slot="{ value, handleChange }">
                <FormItem>
                    <FormLabel>{{ t("categories.form.backgroundColor") }}</FormLabel>
                    <div class="flex flex-wrap gap-2">
                        <button
                            v-for="color in BG_COLORS"
                            :key="color.value"
                            type="button"
                            :title="color.label"
                            class="size-7 rounded-md border-2 transition-all"
                            :class="
                                value === color.value
                                    ? 'border-primary scale-110'
                                    : 'border-transparent hover:border-muted-foreground/50'
                            "
                            :style="{ backgroundColor: color.value }"
                            @click="handleChange(color.value)"
                        />
                    </div>
                    <FormMessage />
                </FormItem>
            </FormField>

            <!-- Text color picker -->
            <FormField name="textColor" v-slot="{ value, handleChange }">
                <FormItem>
                    <FormLabel>{{ t("categories.form.textColor") }}</FormLabel>
                    <div class="flex flex-wrap gap-2">
                        <button
                            v-for="color in TEXT_COLORS"
                            :key="color.value"
                            type="button"
                            :title="color.label"
                            class="size-7 rounded-full border-2 transition-all"
                            :class="
                                value === color.value
                                    ? 'border-primary scale-110'
                                    : 'border-transparent hover:border-muted-foreground/50'
                            "
                            :style="{ backgroundColor: color.value }"
                            @click="handleChange(color.value)"
                        />
                    </div>
                    <FormMessage />
                </FormItem>
            </FormField>

            <!-- Live preview badge -->
            <div>
                <p class="text-sm font-medium mb-2">{{ t("categories.form.preview") }}</p>
                <span
                    class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-sm font-medium"
                    :style="{ backgroundColor: previewBg, color: previewText }"
                >
                    <component v-if="previewIcon" :is="previewIcon" class="size-3.5 shrink-0" />
                    {{ previewName }}
                </span>
            </div>
        </template>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{
                initialName !== undefined
                    ? t("categories.form.submitEdit")
                    : t("categories.form.submit")
            }}
        </Button>
    </form>
</template>
