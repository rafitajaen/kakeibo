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
import { Separator } from "@/components/ui/separator";
import GoogleSignInButton from "@/components/auth/GoogleSignInButton.vue";

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const apiError = ref<string | null>(null);

const schema = toTypedSchema(
    z.object({
        email: z.string().email(),
        password: z.string().min(1),
    }),
);

const form = useForm({ validationSchema: schema });

const onSubmit = form.handleSubmit(async (values) => {
    apiError.value = null;
    try {
        await auth.login(values);
        await router.push({ name: "home" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 401) {
            apiError.value = t("auth.errors.invalidCredentials");
        } else if (status === 400) {
            apiError.value = t("auth.errors.emailNotVerified");
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
                <FormLabel>{{ t("auth.login.email") }}</FormLabel>
                <FormControl>
                    <Input
                        type="email"
                        :placeholder="t('auth.login.emailPlaceholder')"
                        autocomplete="email"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <FormField v-slot="{ componentField }" name="password">
            <FormItem>
                <FormLabel>{{ t("auth.login.password") }}</FormLabel>
                <FormControl>
                    <Input
                        type="password"
                        :placeholder="t('auth.login.passwordPlaceholder')"
                        autocomplete="current-password"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <p v-if="apiError" class="text-sm text-destructive">{{ apiError }}</p>

        <Button type="submit" class="w-full" :disabled="form.isSubmitting.value">
            {{ t("auth.login.submit") }}
        </Button>

        <div class="relative my-2">
            <div class="absolute inset-0 flex items-center">
                <Separator />
            </div>
            <div class="relative flex justify-center text-xs uppercase">
                <span class="bg-background text-muted-foreground px-2">
                    {{ t("auth.login.orContinueWith") }}
                </span>
            </div>
        </div>

        <GoogleSignInButton :label="t('auth.login.googleSignIn')" />
    </form>
</template>
