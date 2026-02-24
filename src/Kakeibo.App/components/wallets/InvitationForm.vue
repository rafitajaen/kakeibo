<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

defineProps<{
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    // Emits the validated email address on submit.
    (e: "submit", email: string): void;
}>();

const { t } = useI18n();

const schema = toTypedSchema(
    z.object({
        email: z.string().email(),
    }),
);

const form = useForm({ validationSchema: schema });

const onSubmit = form.handleSubmit((values) => {
    emit("submit", values.email);
});

// Expose form helpers for test-friendliness.
defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="email">
            <FormItem>
                <FormLabel>{{ t("wallets.invitation.email") }}</FormLabel>
                <FormControl>
                    <Input
                        type="email"
                        :placeholder="t('wallets.invitation.emailPlaceholder')"
                        autocomplete="email"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{ t("wallets.invitation.submit") }}
        </Button>
    </form>
</template>
