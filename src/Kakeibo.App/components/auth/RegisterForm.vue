<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { useAuthStore } from "@/stores/auth";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const apiError = ref<string | null>(null);
const successMessage = ref<string | null>(null);

// Supported currencies for the selector.
const currencies = ["EUR", "USD", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "INR", "BRL", "MXN"];

// Password must be at least 8 chars with one uppercase, lowercase, and digit.
const passwordSchema = z
    .string()
    .min(8, t("auth.validation.passwordMinLength"))
    .regex(/[A-Z]/, t("auth.validation.passwordUppercase"))
    .regex(/[a-z]/, t("auth.validation.passwordLowercase"))
    .regex(/[0-9]/, t("auth.validation.passwordDigit"));

const schema = toTypedSchema(
    z
        .object({
            email: z.string().email(),
            password: passwordSchema,
            confirmPassword: z.string(),
            currency: z.string().min(1),
        })
        .refine((data) => data.password === data.confirmPassword, {
            message: t("auth.validation.passwordsMustMatch"),
            path: ["confirmPassword"],
        }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: { currency: "EUR" },
});

const onSubmit = form.handleSubmit(async (values) => {
    apiError.value = null;
    successMessage.value = null;
    try {
        await auth.register({
            email: values.email,
            password: values.password,
            currency: values.currency,
        });
        successMessage.value = t("auth.register.checkEmail");
        // Navigate to login after a brief moment to let the user read the message.
        setTimeout(() => router.push({ name: "login" }), 2000);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            apiError.value = t("auth.errors.emailConflict");
        } else {
            apiError.value = t("auth.errors.unexpected");
        }
    }
});
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="email">
            <FormItem>
                <FormLabel>{{ t("auth.register.email") }}</FormLabel>
                <FormControl>
                    <Input
                        type="email"
                        :placeholder="t('auth.register.emailPlaceholder')"
                        autocomplete="email"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="password">
            <FormItem>
                <FormLabel>{{ t("auth.register.password") }}</FormLabel>
                <FormControl>
                    <Input
                        type="password"
                        :placeholder="t('auth.register.passwordPlaceholder')"
                        autocomplete="new-password"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="confirmPassword">
            <FormItem>
                <FormLabel>{{ t("auth.register.confirmPassword") }}</FormLabel>
                <FormControl>
                    <Input
                        type="password"
                        :placeholder="t('auth.register.confirmPasswordPlaceholder')"
                        autocomplete="new-password"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="currency">
            <FormItem>
                <FormLabel>{{ t("auth.register.currency") }}</FormLabel>
                <FormControl>
                    <select
                        v-bind="componentField"
                        class="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring flex h-10 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        <option value="" disabled>
                            {{ t("auth.register.currencyPlaceholder") }}
                        </option>
                        <option v-for="currency in currencies" :key="currency" :value="currency">
                            {{ currency }}
                        </option>
                    </select>
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <p v-if="apiError" class="text-sm text-destructive">{{ apiError }}</p>
        <p v-if="successMessage" class="text-sm text-green-600">
            {{ successMessage }}
        </p>

        <Button type="submit" class="w-full" :disabled="form.isSubmitting.value">
            {{ t("auth.register.submit") }}
        </Button>
    </form>
</template>
