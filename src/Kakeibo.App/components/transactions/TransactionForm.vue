<script setup lang="ts">
import { computed, watch } from "vue";
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
import CalculatorInput from "@/components/transactions/CalculatorInput.vue";
import CategorySelector from "@/components/categories/CategorySelector.vue";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";

const TRANSACTION_TYPES = ["Income", "Expense", "Transfer"] as const;

interface TransactionFormValues {
    type: string;
    amount: number;
    description: string;
    date: string;
    categoryId: string;
    destinationWalletId?: string | null;
}

const props = defineProps<{
    // When provided, the form renders in edit mode (type field becomes readonly).
    initialData?: Partial<TransactionFormValues>;
    isSubmitting?: boolean;
    // Active categories for the CategorySelector dropdown.
    categories: Category[];
    // All non-archived wallets for the destination wallet selector (Transfer).
    wallets: Wallet[];
    // Source wallet ID to exclude from the destination list.
    sourceWalletId?: string;
}>();

const emit = defineEmits<{
    (e: "submit", values: TransactionFormValues): void;
}>();

const { t } = useI18n();

// Edit mode is determined by whether initialData carries a type value.
const isEditMode = computed(() => props.initialData?.type !== undefined);

const schema = toTypedSchema(
    z.object({
        type: z.enum(["Income", "Expense", "Transfer"]),
        amount: z.number().min(0.01).max(999_999_999.99),
        description: z.string().min(1).max(500),
        date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, "Date must be YYYY-MM-DD"),
        categoryId: z.string().uuid(),
        destinationWalletId: z.string().uuid().nullable().optional(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        type: props.initialData?.type ?? "Expense",
        amount: props.initialData?.amount ?? 0,
        description: props.initialData?.description ?? "",
        date: props.initialData?.date ?? new Date().toISOString().slice(0, 10),
        categoryId: props.initialData?.categoryId ?? "",
        destinationWalletId: props.initialData?.destinationWalletId ?? null,
    },
});

const currentType = computed(() => form.values.type as string);
const isTransfer = computed(() => currentType.value === "Transfer");

// Available destination wallets — excludes the source wallet and archived wallets.
const destinationWallets = computed(() =>
    props.wallets.filter((w) => w.id !== props.sourceWalletId && !w.isArchived),
);

// Clear destination wallet when switching away from Transfer type.
watch(currentType, (newType) => {
    if (newType !== "Transfer") {
        form.setFieldValue("destinationWalletId", null);
    }
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", {
        type: values.type,
        amount: values.amount,
        description: values.description,
        date: values.date,
        categoryId: values.categoryId,
        destinationWalletId: values.destinationWalletId ?? null,
    });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Transaction type (readonly in edit mode) -->
        <FormField v-slot="{ componentField }" name="type">
            <FormItem>
                <FormLabel>{{ t("transactions.form.type") }}</FormLabel>
                <FormControl>
                    <Select v-bind="componentField" :disabled="isEditMode">
                        <SelectTrigger>
                            <SelectValue :placeholder="t('transactions.form.type')" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="txType in TRANSACTION_TYPES"
                                :key="txType"
                                :value="txType"
                            >
                                {{ t(`transactions.type.${txType.toLowerCase()}`) }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Amount (with calculator expression support) -->
        <FormField name="amount" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("transactions.form.amount") }}</FormLabel>
                <FormControl>
                    <CalculatorInput
                        :model-value="(field.value as number) ?? 0"
                        @update:model-value="form.setFieldValue('amount', $event)"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Description -->
        <FormField v-slot="{ componentField }" name="description">
            <FormItem>
                <FormLabel>{{ t("transactions.form.description") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('transactions.form.descriptionPlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Date -->
        <FormField v-slot="{ componentField }" name="date">
            <FormItem>
                <FormLabel>{{ t("transactions.form.date") }}</FormLabel>
                <FormControl>
                    <Input type="date" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Category -->
        <FormField name="categoryId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("transactions.form.category") }}</FormLabel>
                <FormControl>
                    <CategorySelector
                        :categories="categories"
                        :placeholder="t('transactions.form.categoryPlaceholder')"
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('categoryId', $event)"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Destination wallet (Transfer only) -->
        <FormField v-if="isTransfer" name="destinationWalletId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("transactions.form.destinationWallet") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string | null) ?? ''"
                        @update:model-value="
                            form.setFieldValue('destinationWalletId', $event || null)
                        "
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('transactions.form.destinationWalletPlaceholder')"
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="wallet in destinationWallets"
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
            {{ isEditMode ? t("transactions.form.submitEdit") : t("transactions.form.submit") }}
        </Button>
    </form>
</template>
