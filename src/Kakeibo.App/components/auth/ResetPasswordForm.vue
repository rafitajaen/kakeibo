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

const props = defineProps<{
    // The reset token extracted from the URL query parameter.
    token: string;
}>();

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const apiError = ref<string | null>(null);
const success = ref(false);

const passwordSchema = z
    .string()
    .min(8, "Password must be at least 8 characters")
    .regex(/[A-Z]/, "Password must contain at least one uppercase letter")
    .regex(/[a-z]/, "Password must contain at least one lowercase letter")
    .regex(/[0-9]/, "Password must contain at least one digit");

const schema = toTypedSchema(
    z
        .object({
            newPassword: passwordSchema,
            confirmPassword: z.string(),
        })
        .refine((data) => data.newPassword === data.confirmPassword, {
            message: "Passwords do not match",
            path: ["confirmPassword"],
        }),
);

const form = useForm({ validationSchema: schema });

const onSubmit = form.handleSubmit(async (values) => {
    apiError.value = null;
    try {
        await auth.resetPassword(props.token, values.newPassword, values.confirmPassword);
        success.value = true;
        setTimeout(() => router.push({ name: "login" }), 2000);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
            apiError.value = t("auth.errors.tokenExpired");
        } else {
            apiError.value = t("auth.errors.unexpected");
        }
    }
});
</script>

<template>
    <div v-if="success">
        <p class="text-sm text-green-600">
            {{ t("auth.resetPassword.success") }}
        </p>
    </div>

    <form v-else @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="newPassword">
            <FormItem>
                <FormLabel>{{ t("auth.resetPassword.newPassword") }}</FormLabel>
                <FormControl>
                    <Input
                        type="password"
                        :placeholder="t('auth.resetPassword.newPasswordPlaceholder')"
                        autocomplete="new-password"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="confirmPassword">
            <FormItem>
                <FormLabel>{{ t("auth.resetPassword.confirmPassword") }}</FormLabel>
                <FormControl>
                    <Input
                        type="password"
                        :placeholder="t('auth.resetPassword.confirmPasswordPlaceholder')"
                        autocomplete="new-password"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <p v-if="apiError" class="text-sm text-destructive">{{ apiError }}</p>

        <Button type="submit" class="w-full" :disabled="form.isSubmitting.value">
            {{ t("auth.resetPassword.submit") }}
        </Button>
    </form>
</template>
