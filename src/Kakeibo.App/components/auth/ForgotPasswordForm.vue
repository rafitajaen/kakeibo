<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { useAuthStore } from "@/stores/auth";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const { t } = useI18n();
const auth = useAuthStore();

// The API always returns 202 regardless of whether the email exists (security).
const sent = ref(false);

const schema = toTypedSchema(
    z.object({
        email: z.string().email(),
    }),
);

const form = useForm({ validationSchema: schema });

const onSubmit = form.handleSubmit(async (values) => {
    await auth.forgotPassword(values.email);
    // Show the same message whether or not the email exists.
    sent.value = true;
});
</script>

<template>
    <div v-if="sent">
        <p class="text-sm text-muted-foreground">
            {{ t("auth.forgotPassword.sent") }}
        </p>
    </div>

    <form v-else @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="email">
            <FormItem>
                <FormLabel>{{ t("auth.forgotPassword.email") }}</FormLabel>
                <FormControl>
                    <Input
                        type="email"
                        :placeholder="t('auth.forgotPassword.emailPlaceholder')"
                        autocomplete="email"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="form.isSubmitting.value">
            {{ t("auth.forgotPassword.submit") }}
        </Button>
    </form>
</template>
