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

const props = defineProps<{
    // When provided, the form renders in edit mode (only name is editable; type is immutable).
    initialName?: string;
    isSubmitting?: boolean;
}>();

const emit = defineEmits<{
    // Emits the validated form values on submit.
    (e: "submit", values: { name: string; type: string }): void;
}>();

const { t } = useI18n();

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
        type: z.enum(["Personal", "Shared"]),
    }),
);

const form = useForm({
    validationSchema: schema,
    initialValues: { name: props.initialName ?? "", type: "Personal" as const },
});

const onSubmit = form.handleSubmit((values) => {
    emit("submit", { name: values.name, type: values.type });
});

// Expose form helpers so tests can set field values and trigger submission
// without relying on DOM event propagation through the VeeValidate / shadcn-vue stack.
defineExpose({ setFieldValue: form.setFieldValue, onSubmit });
</script>

<template>
    <form @submit.prevent="onSubmit" class="space-y-4">
        <FormField v-slot="{ componentField }" name="name">
            <FormItem>
                <FormLabel>{{ t("wallets.form.name") }}</FormLabel>
                <FormControl>
                    <Input
                        type="text"
                        :placeholder="t('wallets.form.namePlaceholder')"
                        autocomplete="off"
                        v-bind="componentField"
                    />
                </FormControl>
                <FormMessage />
            </FormItem>
        </FormField>

        <!-- Type selector only visible in create mode; type is immutable after creation. -->
        <FormField v-if="initialName === undefined" v-slot="{ componentField }" name="type">
            <FormItem>
                <FormLabel>{{ t("wallets.form.type") }}</FormLabel>
                <Select v-bind="componentField">
                    <FormControl>
                        <SelectTrigger>
                            <SelectValue :placeholder="t('wallets.form.typePlaceholder')" />
                        </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                        <SelectItem value="Personal">{{ t("wallets.type.personal") }}</SelectItem>
                        <SelectItem value="Shared">{{ t("wallets.type.shared") }}</SelectItem>
                    </SelectContent>
                </Select>
                <FormMessage />
            </FormItem>
        </FormField>

        <Button type="submit" class="w-full" :disabled="isSubmitting ?? form.isSubmitting.value">
            {{
                initialName !== undefined ? t("wallets.form.submitEdit") : t("wallets.form.submit")
            }}
        </Button>
    </form>
</template>
