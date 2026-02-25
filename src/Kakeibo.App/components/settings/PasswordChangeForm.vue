<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useSettingsStore } from "@/stores/settings";

const { t } = useI18n();
const settingsStore = useSettingsStore();

const isSaving = ref(false);
const saved = ref(false);
const errorMessage = ref("");

const schema = toTypedSchema(
    z
        .object({
            currentPassword: z.string().min(1),
            newPassword: z
                .string()
                .min(8)
                .max(128)
                .regex(/[A-Z]/, "Must contain at least one uppercase letter")
                .regex(/[a-z]/, "Must contain at least one lowercase letter")
                .regex(/[0-9]/, "Must contain at least one digit"),
            confirmPassword: z.string().min(1),
        })
        .refine((d) => d.newPassword === d.confirmPassword, {
            message: "Passwords do not match",
            path: ["confirmPassword"],
        }),
);

const { handleSubmit, defineField, errors, resetForm } = useForm({ validationSchema: schema });
const [currentPassword, currentPasswordAttrs] = defineField("currentPassword");
const [newPassword, newPasswordAttrs] = defineField("newPassword");
const [confirmPassword, confirmPasswordAttrs] = defineField("confirmPassword");

const onSubmit = handleSubmit(async (values) => {
    isSaving.value = true;
    saved.value = false;
    errorMessage.value = "";
    try {
        await settingsStore.changePassword({
            currentPassword: values.currentPassword,
            newPassword: values.newPassword,
            confirmPassword: values.confirmPassword,
        });
        saved.value = true;
        resetForm();
        setTimeout(() => {
            saved.value = false;
        }, 2000);
    } catch {
        errorMessage.value = t("common.errorGeneric");
    } finally {
        isSaving.value = false;
    }
});
</script>

<template>
    <form class="space-y-4" @submit="onSubmit">
        <div class="space-y-2">
            <Label for="current-password">{{ t("settings.security.currentPassword") }}</Label>
            <Input
                id="current-password"
                v-model="currentPassword"
                v-bind="currentPasswordAttrs"
                type="password"
                :disabled="isSaving"
            />
            <p v-if="errors.currentPassword" class="text-sm text-destructive">
                {{ errors.currentPassword }}
            </p>
        </div>
        <div class="space-y-2">
            <Label for="new-password">{{ t("settings.security.newPassword") }}</Label>
            <Input
                id="new-password"
                v-model="newPassword"
                v-bind="newPasswordAttrs"
                type="password"
                :disabled="isSaving"
            />
            <p v-if="errors.newPassword" class="text-sm text-destructive">
                {{ errors.newPassword }}
            </p>
        </div>
        <div class="space-y-2">
            <Label for="confirm-password">{{ t("settings.security.confirmPassword") }}</Label>
            <Input
                id="confirm-password"
                v-model="confirmPassword"
                v-bind="confirmPasswordAttrs"
                type="password"
                :disabled="isSaving"
            />
            <p v-if="errors.confirmPassword" class="text-sm text-destructive">
                {{ errors.confirmPassword }}
            </p>
        </div>
        <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>
        <Button type="submit" :disabled="isSaving">
            {{ saved ? t("settings.security.saved") : t("settings.security.save") }}
        </Button>
    </form>
</template>
