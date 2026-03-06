<script setup lang="ts">
import { ref, computed, watch } from "vue";
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
import { useAuthStore } from "@/stores/auth";
import { useSettingsStore } from "@/stores/settings";

const { t } = useI18n();
const authStore = useAuthStore();
const settingsStore = useSettingsStore();

const isSaved = ref(false);

const schema = toTypedSchema(
    z.object({
        weekStartDay: z.coerce.number().int().min(0).max(6),
        monthStartDay: z.coerce.number().int().min(1).max(28),
        currencyDecimalSeparator: z.enum([".", ","]),
        currencyGroupSeparator: z.enum([",", ".", " ", ""]),
        currencySymbolPosition: z.enum(["before", "after"]),
        currencyDisplay: z.enum(["symbol", "code", "none"]),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: {
        weekStartDay: authStore.user?.weekStartDay ?? 1,
        monthStartDay: authStore.user?.monthStartDay ?? 1,
        currencyDecimalSeparator: (authStore.user?.currencyDecimalSeparator ?? ".") as "." | ",",
        currencyGroupSeparator: (authStore.user?.currencyGroupSeparator ?? ",") as
            | ","
            | "."
            | " "
            | "",
        currencySymbolPosition: (authStore.user?.currencySymbolPosition ?? "before") as
            | "before"
            | "after",
        currencyDisplay: (authStore.user?.currencyDisplay ?? "symbol") as
            | "symbol"
            | "code"
            | "none",
    },
});

// Live preview of currency formatting based on current form values.
const previewAmount = computed(() => {
    const values = form.values;
    const decimal = values.currencyDecimalSeparator ?? ".";
    const group = values.currencyGroupSeparator ?? ",";
    const pos = values.currencySymbolPosition ?? "before";
    const display = values.currencyDisplay ?? "symbol";
    const currency = authStore.user?.currency ?? "USD";

    // Build a sample number: 1234.56
    const intPart = group !== "" ? `1${group}234` : "1234";
    const formatted = `${intPart}${decimal}56`;

    let symbol = "";
    if (display === "symbol") {
        const parts = new Intl.NumberFormat(undefined, {
            style: "currency",
            currency,
            currencyDisplay: "symbol",
        }).formatToParts(0);
        symbol = parts.find((p) => p.type === "currency")?.value ?? currency;
    } else if (display === "code") {
        symbol = currency;
    }

    if (!symbol) return formatted;
    return pos === "before" ? `${symbol}${formatted}` : `${formatted} ${symbol}`;
});

const onSubmit = form.handleSubmit(async (values) => {
    await settingsStore.updateDisplayPreferences({
        weekStartDay: values.weekStartDay,
        monthStartDay: values.monthStartDay,
        currencyDecimalSeparator: values.currencyDecimalSeparator,
        currencyGroupSeparator: values.currencyGroupSeparator,
        currencySymbolPosition: values.currencySymbolPosition,
        currencyDisplay: values.currencyDisplay,
    });
    // Refresh current user so auth store reflects the new preferences immediately.
    await authStore.fetchCurrentUser();
    isSaved.value = true;
    setTimeout(() => (isSaved.value = false), 3000);
});
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-6">
        <!-- Week & Month start -->
        <div class="space-y-4">
            <h3 class="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                {{ t("settings.display.weekMonth") }}
            </h3>

            <FormField v-slot="{ componentField }" name="weekStartDay">
                <FormItem>
                    <FormLabel>{{ t("settings.display.weekStartDay") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                            <SelectItem value="1">{{
                                t("settings.display.days.monday")
                            }}</SelectItem>
                            <SelectItem value="2">{{
                                t("settings.display.days.tuesday")
                            }}</SelectItem>
                            <SelectItem value="3">{{
                                t("settings.display.days.wednesday")
                            }}</SelectItem>
                            <SelectItem value="4">{{
                                t("settings.display.days.thursday")
                            }}</SelectItem>
                            <SelectItem value="5">{{
                                t("settings.display.days.friday")
                            }}</SelectItem>
                            <SelectItem value="6">{{
                                t("settings.display.days.saturday")
                            }}</SelectItem>
                            <SelectItem value="0">{{
                                t("settings.display.days.sunday")
                            }}</SelectItem>
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <FormField v-slot="{ componentField }" name="monthStartDay">
                <FormItem>
                    <FormLabel>{{ t("settings.display.monthStartDay") }}</FormLabel>
                    <FormControl>
                        <Input type="number" min="1" max="28" v-bind="componentField" />
                    </FormControl>
                    <FormMessage />
                </FormItem>
            </FormField>
        </div>

        <!-- Currency format -->
        <div class="space-y-4">
            <h3 class="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                {{ t("settings.display.currencyFormat") }}
            </h3>

            <FormField v-slot="{ componentField }" name="currencyDecimalSeparator">
                <FormItem>
                    <FormLabel>{{ t("settings.display.decimalSeparator") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                            <SelectItem value="."
                                >{{ t("settings.display.separators.dot") }} (.)</SelectItem
                            >
                            <SelectItem value=","
                                >{{ t("settings.display.separators.comma") }} (,)</SelectItem
                            >
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <FormField v-slot="{ componentField }" name="currencyGroupSeparator">
                <FormItem>
                    <FormLabel>{{ t("settings.display.groupSeparator") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                            <SelectItem value=","
                                >{{ t("settings.display.separators.comma") }} (,)</SelectItem
                            >
                            <SelectItem value="."
                                >{{ t("settings.display.separators.dot") }} (.)</SelectItem
                            >
                            <SelectItem value=" ">{{
                                t("settings.display.separators.space")
                            }}</SelectItem>
                            <SelectItem value="">{{
                                t("settings.display.separators.none")
                            }}</SelectItem>
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <FormField v-slot="{ componentField }" name="currencySymbolPosition">
                <FormItem>
                    <FormLabel>{{ t("settings.display.symbolPosition") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                            <SelectItem value="before">{{
                                t("settings.display.positions.before")
                            }}</SelectItem>
                            <SelectItem value="after">{{
                                t("settings.display.positions.after")
                            }}</SelectItem>
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <FormField v-slot="{ componentField }" name="currencyDisplay">
                <FormItem>
                    <FormLabel>{{ t("settings.display.currencyDisplayMode") }}</FormLabel>
                    <Select v-bind="componentField">
                        <FormControl>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                            <SelectItem value="symbol">{{
                                t("settings.display.displayModes.symbol")
                            }}</SelectItem>
                            <SelectItem value="code">{{
                                t("settings.display.displayModes.code")
                            }}</SelectItem>
                            <SelectItem value="none">{{
                                t("settings.display.displayModes.none")
                            }}</SelectItem>
                        </SelectContent>
                    </Select>
                    <FormMessage />
                </FormItem>
            </FormField>

            <!-- Live preview -->
            <div class="rounded-md border bg-muted/40 px-4 py-3">
                <p class="text-xs text-muted-foreground mb-1">
                    {{ t("settings.display.preview") }}
                </p>
                <p class="text-lg font-semibold tabular-nums">{{ previewAmount }}</p>
            </div>
        </div>

        <div class="flex items-center gap-3">
            <Button type="submit" :disabled="form.isSubmitting.value">
                {{ t("settings.display.save") }}
            </Button>
            <span v-if="isSaved" class="text-sm text-green-600 dark:text-green-400">
                {{ t("settings.display.saved") }}
            </span>
        </div>
    </form>
</template>
