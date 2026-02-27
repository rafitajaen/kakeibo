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
import type { Budget, CreateBudgetData, UpdateBudgetData } from "@/stores/budgets";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";

const props = defineProps<{
    // When provided, the form renders in edit mode pre-filled with this budget.
    budget?: Budget;
    categories: Category[];
    wallets: Wallet[];
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (e: "submit", values: CreateBudgetData | UpdateBudgetData): void;
}>();

const { t } = useI18n();

const isEditMode = props.budget !== undefined;

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
        categoryId: z.string().uuid(),
        limit: z.number().gt(0).lte(999_999_999.99),
        startDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, "Date must be YYYY-MM-DD"),
        endDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, "Date must be YYYY-MM-DD"),
        walletId: z.string().uuid().nullable().optional(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        name: props.budget?.name ?? "",
        categoryId: props.budget?.categoryId ?? "",
        limit: props.budget?.limit ?? 0,
        startDate: props.budget?.startDate ?? new Date().toISOString().slice(0, 10),
        endDate: props.budget?.endDate ?? "",
        walletId: props.budget?.walletId ?? null,
    },
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", {
        name: values.name,
        categoryId: values.categoryId,
        limit: values.limit,
        startDate: values.startDate,
        endDate: values.endDate,
        walletId: values.walletId ?? null,
    });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Budget name -->
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.budgets.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Category -->
        <FormField name="categoryId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.category") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="
                            (v: unknown) => form.setFieldValue('categoryId', v as string)
                        "
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('wallets.budgets.form.categoryPlaceholder')"
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem v-for="cat in categories" :key="cat.id" :value="cat.id">
                                {{ cat.name }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Spending limit -->
        <FormField v-slot="{ componentField }" name="limit">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.limit") }}</FormLabel>
                <FormControl>
                    <Input
                        type="number"
                        step="0.01"
                        min="0.01"
                        :placeholder="t('wallets.budgets.form.limitPlaceholder')"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Start date -->
        <FormField v-slot="{ componentField }" name="startDate">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.startDate") }}</FormLabel>
                <FormControl>
                    <Input type="date" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- End date -->
        <FormField v-slot="{ componentField }" name="endDate">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.endDate") }}</FormLabel>
                <FormControl>
                    <Input type="date" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Wallet filter (optional) -->
        <FormField name="walletId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.budgets.form.wallet") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string | null) ?? ''"
                        @update:model-value="
                            (v: unknown) => form.setFieldValue('walletId', (v as string) || null)
                        "
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('wallets.budgets.form.walletPlaceholder')"
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="">
                                {{ t("wallets.budgets.allWallets") }}
                            </SelectItem>
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
            {{
                isEditMode ? t("wallets.budgets.form.submitEdit") : t("wallets.budgets.form.submit")
            }}
        </Button>
    </form>
</template>
