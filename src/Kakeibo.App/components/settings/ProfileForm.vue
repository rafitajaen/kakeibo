<script setup lang="ts">
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
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

const isSaving = ref(false);
const saved = ref(false);

const SUPPORTED_CURRENCIES = [
    "USD",
    "EUR",
    "GBP",
    "JPY",
    "CAD",
    "AUD",
    "CHF",
    "CNY",
    "INR",
    "BRL",
    "MXN",
];

const schema = toTypedSchema(
    z.object({
        name: z.string().max(100).optional(),
        currency: z.string().length(3).optional(),
    }),
);

const initialValues = computed(() => ({
    name: authStore.user?.name ?? "",
    currency: authStore.user?.currency ?? "USD",
}));

const { handleSubmit, defineField, errors } = useForm({
    validationSchema: schema,
    initialValues: initialValues.value,
});

const [name, nameAttrs] = defineField("name");
const [currency, currencyAttrs] = defineField("currency");

const onSubmit = handleSubmit(async (values) => {
    isSaving.value = true;
    saved.value = false;
    try {
        await settingsStore.updateProfile({
            name: values.name || null,
            currency: values.currency,
        });
        if (authStore.user) {
            authStore.user.name = values.name || null;
            if (values.currency) authStore.user.currency = values.currency;
        }
        saved.value = true;
        setTimeout(() => {
            saved.value = false;
        }, 2000);
    } finally {
        isSaving.value = false;
    }
});
</script>

<template>
    <form class="space-y-4" @submit="onSubmit">
        <div class="space-y-2">
            <Label for="profile-name">{{ t("settings.profile.name") }}</Label>
            <Input
                id="profile-name"
                v-model="name"
                v-bind="nameAttrs"
                :placeholder="t('settings.profile.namePlaceholder')"
                :disabled="isSaving"
            />
            <p v-if="errors.name" class="text-sm text-destructive">{{ errors.name }}</p>
        </div>
        <div class="space-y-2">
            <Label for="profile-currency">{{ t("settings.profile.currency") }}</Label>
            <Select v-model="currency" v-bind="currencyAttrs" :disabled="isSaving">
                <SelectTrigger id="profile-currency">
                    <SelectValue :placeholder="t('settings.profile.currencyPlaceholder')" />
                </SelectTrigger>
                <SelectContent>
                    <SelectItem v-for="c in SUPPORTED_CURRENCIES" :key="c" :value="c">
                        {{ c }}
                    </SelectItem>
                </SelectContent>
            </Select>
        </div>
        <Button type="submit" :disabled="isSaving">
            {{ saved ? t("settings.profile.saved") : t("settings.profile.save") }}
        </Button>
    </form>
</template>
