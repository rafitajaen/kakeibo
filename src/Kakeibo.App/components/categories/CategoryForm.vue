<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const props = defineProps<{
    // When provided, the form renders in edit mode pre-filled with this name.
    initialName?: string;
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    (e: "submit", values: { name: string }): void;
}>();

const { t } = useI18n();

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(50),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: { name: props.initialName ?? "" },
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", { name: values.name });
});

defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("categories.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('categories.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{
                initialName !== undefined
                    ? t("categories.form.submitEdit")
                    : t("categories.form.submit")
            }}
        </Button>
    </form>
</template>
