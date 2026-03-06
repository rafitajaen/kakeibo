<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
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
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { ChevronDown } from "lucide-vue-next";
import IconPicker from "@/components/common/IconPicker.vue";
import ColorPicker from "@/components/common/ColorPicker.vue";

const props = defineProps<{
    // When provided, the form renders in edit mode (type is immutable; no initial balance).
    initialName?: string;
    initialIcon?: string | null;
    initialBackgroundColor?: string | null;
    initialTextColor?: string | null;
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (
        e: "submit",
        values: {
            name: string;
            type: string;
            initialBalance?: number;
            icon?: string | null;
            backgroundColor?: string | null;
            textColor?: string | null;
        },
    ): void;
}>();

const { t } = useI18n();
const isEditMode = props.initialName !== undefined;
const showCustomize = ref(isEditMode && !!(props.initialIcon || props.initialBackgroundColor));

// Internal state for icon and colors (not managed by vee-validate)
const selectedIcon = ref<string | null>(props.initialIcon ?? null);
const bgColor = ref<string>(props.initialBackgroundColor ?? "#3B82F6");
const textColor = ref<string>(props.initialTextColor ?? "#FFFFFF");

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
        type: z.enum(["Personal", "Shared"]),
        initialBalance: z.coerce.number().min(0).max(999_999_999.99).optional(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: { name: props.initialName ?? "", type: "Personal" as const, initialBalance: 0 },
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", {
        name: values.name,
        type: values.type,
        initialBalance: isEditMode ? undefined : (values.initialBalance ?? 0),
        icon: selectedIcon.value,
        backgroundColor: bgColor.value || null,
        textColor: textColor.value || null,
    });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("wallets.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Type selector only visible in create mode; type is immutable after creation. -->
        <FormField v-if="!isEditMode" v-slot="{ componentField }" name="type">
            <FormItem>
                <FormLabel>{{ t("wallets.form.type") }}</FormLabel>
                <Select v-bind="componentField">
                    <FormControl>
                        <SelectTrigger>
                            <SelectValue :placeholder="t('wallets.form.typePlaceholder')" />
                        </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                        <SelectItem value="Personal">{{ t("wallets.type.personal") }}</SelectItem>
                        <SelectItem value="Shared">{{ t("wallets.type.shared") }}</SelectItem>
                    </SelectContent>
                </Select>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Initial balance only in create mode -->
        <FormField v-if="!isEditMode" v-slot="{ componentField }" name="initialBalance">
            <FormItem>
                <FormLabel>{{ t("wallets.form.initialBalance") }}</FormLabel>
                <FormControl>
                    <Input
                        type="number"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Customization (icon + colors) — collapsible -->
        <Collapsible v-model:open="showCustomize">
            <CollapsibleTrigger as-child>
                <Button variant="ghost" type="button" class="w-full justify-between text-sm">
                    {{ t("wallets.form.customize") }}
                    <ChevronDown
                        class="size-4 transition-transform"
                        :class="showCustomize ? 'rotate-180' : ''"
                    />
                </Button>
            </CollapsibleTrigger>
            <CollapsibleContent class="space-y-4 pt-2">
                <div>
                    <p class="text-sm font-medium mb-2">{{ t("wallets.form.icon") }}</p>
                    <IconPicker v-model="selectedIcon" />
                </div>
                <div>
                    <p class="text-sm font-medium mb-2">{{ t("wallets.form.colors") }}</p>
                    <ColorPicker
                        v-model:background-color="bgColor"
                        v-model:text-color="textColor"
                        :icon-name="selectedIcon"
                        :wallet-name="form.values.name"
                    />
                </div>
            </CollapsibleContent>
        </Collapsible>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{ isEditMode ? t("wallets.form.submitEdit") : t("wallets.form.submit") }}
        </Button>
    </form>
</template>
