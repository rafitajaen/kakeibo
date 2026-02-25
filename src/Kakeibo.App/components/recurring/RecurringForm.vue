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
import type { RecurringPattern, CreatePatternData, UpdatePatternData } from "@/stores/recurring";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";

const TRANSACTION_TYPES = ["Income", "Expense", "Transfer"] as const;
const FREQUENCIES = ["Daily", "Weekly", "Biweekly", "Monthly", "Yearly"] as const;

const props = defineProps<{
    // When provided, the form renders in edit mode pre-filled with this pattern.
    pattern?: RecurringPattern;
    categories: Category[];
    wallets: Wallet[];
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (e: "submit", values: CreatePatternData | UpdatePatternData): void;
}>();

const { t } = useI18n();

const isEditMode = props.pattern !== undefined;

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
        amount: z.number().min(0.01).max(999_999_999.99),
        description: z.string().min(1).max(500),
        transactionType: z.enum(["Income", "Expense", "Transfer"]),
        frequency: z.enum(["Daily", "Weekly", "Biweekly", "Monthly", "Yearly"]),
        categoryId: z.string().uuid(),
        walletId: z.string().uuid(),
        destinationWalletId: z.string().uuid().nullable().optional(),
        startDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, {
            message: t("wallets.recurring.validation.startDateFormat"),
        }),
        endDate: z
            .string()
            .refine((v) => !v || /^\d{4}-\d{2}-\d{2}$/.test(v), {
                message: t("wallets.recurring.validation.endDateFormat"),
            })
            .nullable()
            .optional(),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        name: props.pattern?.name ?? "",
        amount: props.pattern?.amount ?? 0,
        description: props.pattern?.description ?? "",
        transactionType:
            (props.pattern?.transactionType as "Income" | "Expense" | "Transfer") ?? "Expense",
        frequency:
            (props.pattern?.frequency as "Daily" | "Weekly" | "Biweekly" | "Monthly" | "Yearly") ??
            "Monthly",
        categoryId: props.pattern?.categoryId ?? "",
        walletId: props.pattern?.walletId ?? "",
        destinationWalletId: props.pattern?.destinationWalletId ?? null,
        startDate: props.pattern?.startDate ?? new Date().toISOString().slice(0, 10),
        endDate: props.pattern?.endDate ?? null,
    },
});

const currentType = computed(() => form.values.transactionType as string);
const isTransfer = computed(() => currentType.value === "Transfer");

// Available destination wallets — excludes the selected source wallet.
const destinationWallets = computed(() =>
    props.wallets.filter((w) => w.id !== form.values.walletId && !w.isArchived),
);

// Clear destination wallet when switching away from Transfer type.
watch(currentType, (newType) => {
    if (newType !== "Transfer") {
        form.setFieldValue("destinationWalletId", null);
    }
});

const onSubmit = form.handleSubmit((values) => {
    if (isEditMode) {
        emit("submit", {
            name: values.name,
            amount: values.amount,
            description: values.description,
            transactionType: values.transactionType,
            frequency: values.frequency,
            categoryId: values.categoryId,
            walletId: values.walletId,
            destinationWalletId: values.destinationWalletId ?? null,
            endDate: values.endDate || null,
        } as UpdatePatternData);
    } else {
        emit("submit", {
            name: values.name,
            amount: values.amount,
            description: values.description,
            transactionType: values.transactionType,
            frequency: values.frequency,
            categoryId: values.categoryId,
            walletId: values.walletId,
            destinationWalletId: values.destinationWalletId ?? null,
            startDate: values.startDate,
            endDate: values.endDate || null,
        } as CreatePatternData);
    }
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <!-- Pattern name -->
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.recurring.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Transaction type -->
        <FormField name="transactionType" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.transactionType") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('transactionType', $event)"
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="
                                    t('wallets.recurring.form.transactionTypePlaceholder')
                                "
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="txType in TRANSACTION_TYPES"
                                :key="txType"
                                :value="txType"
                            >
                                {{ t(`wallets.recurring.transactionType.${txType.toLowerCase()}`) }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Frequency -->
        <FormField name="frequency" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.frequency") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('frequency', $event)"
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('wallets.recurring.form.frequencyPlaceholder')"
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem v-for="freq in FREQUENCIES" :key="freq" :value="freq">
                                {{ t(`wallets.recurring.frequency.${freq.toLowerCase()}`) }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Amount -->
        <FormField v-slot="{ componentField }" name="amount">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.amount") }}</FormLabel>
                <FormControl>
                    <Input
                        type="number"
                        step="0.01"
                        min="0.01"
                        :placeholder="t('wallets.recurring.form.amountPlaceholder')"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Description -->
        <FormField v-slot="{ componentField }" name="description">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.description") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.recurring.form.descriptionPlaceholder')"
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
                <FormLabel>{{ t("wallets.recurring.form.category") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('categoryId', $event)"
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('wallets.recurring.form.categoryPlaceholder')"
                            />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="category in categories"
                                :key="category.id"
                                :value="category.id"
                            >
                                {{ category.name }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Wallet -->
        <FormField name="walletId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.wallet") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string) ?? ''"
                        @update:model-value="form.setFieldValue('walletId', $event)"
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="t('wallets.recurring.form.walletPlaceholder')"
                            />
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

        <!-- Destination wallet (Transfer only) -->
        <FormField v-if="isTransfer" name="destinationWalletId" v-slot="{ field }">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.destinationWallet") }}</FormLabel>
                <FormControl>
                    <Select
                        :model-value="(field.value as string | null) ?? ''"
                        @update:model-value="
                            form.setFieldValue('destinationWalletId', $event || null)
                        "
                    >
                        <SelectTrigger>
                            <SelectValue
                                :placeholder="
                                    t('wallets.recurring.form.destinationWalletPlaceholder')
                                "
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

        <!-- Start date (read-only in edit mode) -->
        <FormField v-slot="{ componentField }" name="startDate">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.startDate") }}</FormLabel>
                <FormControl>
                    <Input type="date" :readonly="isEditMode" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- End date (optional) -->
        <FormField v-slot="{ componentField }" name="endDate">
            <FormItem>
                <FormLabel>{{ t("wallets.recurring.form.endDate") }}</FormLabel>
                <FormControl>
                    <Input type="date" v-bind="componentField" />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{
                isEditMode
                    ? t("wallets.recurring.form.submitEdit")
                    : t("wallets.recurring.form.submit")
            }}
        </Button>
    </form>
</template>
