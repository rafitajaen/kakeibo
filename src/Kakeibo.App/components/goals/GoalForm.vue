<script setup lang="ts">
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
import type { Goal, CreateGoalData, UpdateGoalData } from "@/stores/goals";
import type { Wallet } from "@/stores/wallets";

const props = defineProps<{
    // When provided, the form renders in edit mode pre-filled with this goal.
    goal?: Goal;
    wallets: Wallet[];
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (e: "submit", values: CreateGoalData | UpdateGoalData): void;
}>();

const { t } = useI18n();

const isEditMode = props.goal !== undefined;

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
        targetAmount: z.number().gt(0).lte(999_999_999.99),
        deadline: z
            .string()
            // Empty string is treated as "no deadline" — only validate non-empty values
            .refine((v) => !v || /^\d{4}-\d{2}-\d{2}$/.test(v), {
                message: t("wallets.goals.validation.deadlineFormat"),
            })
            // Business constraint: deadline cannot exceed 10 years from today
            .refine(
                (v) => {
                    if (!v) return true;
                    const maxDate = new Date();
                    maxDate.setFullYear(maxDate.getFullYear() + 10);
                    return new Date(v) <= maxDate;
                },
                { message: t("wallets.goals.validation.deadlineMaxYears") },
            )
            .nullable()
            .optional(),
        walletId: z.string().uuid(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        name: props.goal?.name ?? "",
        targetAmount: props.goal?.targetAmount ?? 0,
        deadline: props.goal?.deadline ?? null,
        walletId: props.goal?.walletId ?? "",
    },
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", {
        name: values.name,
        targetAmount: values.targetAmount,
        // Normalize empty string (cleared date input) to null
        deadline: values.deadline || null,
        walletId: values.walletId,
    });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Goal name -->
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("wallets.goals.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.goals.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Target amount -->
        <FormField v-slot="{ componentField }" name="targetAmount">
            <FormItem>
                <FormLabel>{{ t("wallets.goals.form.targetAmount") }}</FormLabel>
                <FormControl>
                    <Input
                        type="number"
                        step="0.01"
                        min="0.01"
                        :placeholder="t('wallets.goals.form.targetAmountPlaceholder')"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Deadline (optional) -->
        <FormField v-slot="{ componentField }" name="deadline">
            <FormItem>
                <FormLabel>{{ t("wallets.goals.form.deadline") }}</FormLabel>
                <FormControl>
                    <Input type="date" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Wallet (required) -->
        <FormField name="walletId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.goals.form.wallet") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('walletId', $event)"
                    >
                        <SelectTrigger>
                            <SelectValue :placeholder="t('wallets.goals.form.walletPlaceholder')" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="wallet in wallets"
                                :key="wallet.id"
                                :value="wallet.id"
                            >
                                {{ wallet.name }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{ isEditMode ? t("wallets.goals.form.submitEdit") : t("wallets.goals.form.submit") }}
        </Button>
    </form>
</template>
