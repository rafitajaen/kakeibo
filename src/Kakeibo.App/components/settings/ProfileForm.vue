<script setup lang="ts">
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import api from "@/lib/axios";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
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
const isUploadingAvatar = ref(false);
const avatarError = ref<string | null>(null);

const usernameInput = ref(authStore.user?.username ?? "");
const isSavingUsername = ref(false);
const usernameSaved = ref(false);
const usernameError = ref<string | null>(null);

async function saveUsername() {
    if (!usernameInput.value || usernameInput.value === authStore.user?.username) return;
    isSavingUsername.value = true;
    usernameError.value = null;
    usernameSaved.value = false;
    try {
        await authStore.updateUsername(usernameInput.value);
        usernameSaved.value = true;
        setTimeout(() => {
            usernameSaved.value = false;
        }, 2000);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            usernameError.value = t("settings.profile.usernameConflict");
        } else {
            usernameError.value = t("common.errorGeneric");
        }
    } finally {
        isSavingUsername.value = false;
    }
}

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

const initials = computed(() => {
    const user = authStore.user;
    if (!user) return "?";
    if (user.name) {
        return user.name
            .split(" ")
            .map((s) => s[0])
            .join("")
            .toUpperCase()
            .slice(0, 2);
    }
    return user.email[0].toUpperCase();
});

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

// Validates and uploads avatar via FormData
async function handleAvatarChange(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const allowed = ["image/jpeg", "image/png", "image/webp"];
    if (!allowed.includes(file.type)) {
        avatarError.value = t("settings.profile.avatarTypeError");
        return;
    }
    if (file.size > 5 * 1024 * 1024) {
        avatarError.value = t("settings.profile.avatarSizeError");
        return;
    }

    avatarError.value = null;
    isUploadingAvatar.value = true;
    try {
        const formData = new FormData();
        formData.append("file", file);
        const res = await api.post<{ avatarUrl: string }>("/api/users/me/avatar", formData, {
            headers: { "Content-Type": "multipart/form-data" },
        });
        if (authStore.user) {
            authStore.user.avatarUrl = res.data.avatarUrl;
        }
    } catch {
        avatarError.value = t("settings.profile.avatarUploadError");
    } finally {
        isUploadingAvatar.value = false;
    }
}
</script>

<template>
    <div class="space-y-6">
        <!-- Avatar upload -->
        <div class="flex items-center gap-4">
            <label class="cursor-pointer group relative">
                <Avatar class="size-16 rounded-full">
                    <AvatarImage v-if="authStore.user?.avatarUrl" :src="authStore.user.avatarUrl" />
                    <AvatarFallback class="text-lg">{{ initials }}</AvatarFallback>
                </Avatar>
                <div
                    class="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                    <span class="text-white text-xs font-medium">{{
                        isUploadingAvatar ? "..." : t("settings.profile.changeAvatar")
                    }}</span>
                </div>
                <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    class="hidden"
                    :disabled="isUploadingAvatar"
                    @change="handleAvatarChange"
                />
            </label>
            <div>
                <p class="text-sm font-medium">{{ t("settings.profile.avatar") }}</p>
                <p class="text-xs text-muted-foreground">{{ t("settings.profile.avatarHint") }}</p>
                <p v-if="avatarError" class="text-xs text-destructive">{{ avatarError }}</p>
            </div>
        </div>

        <!-- Username -->
        <div class="space-y-2">
            <Label for="profile-username">{{ t("settings.profile.username") }}</Label>
            <div class="flex gap-2">
                <Input
                    id="profile-username"
                    v-model="usernameInput"
                    :placeholder="t('settings.profile.usernamePlaceholder')"
                    :disabled="isSavingUsername"
                />
                <Button
                    type="button"
                    variant="outline"
                    :disabled="isSavingUsername || usernameInput === authStore.user?.username"
                    @click="saveUsername"
                >
                    {{
                        usernameSaved
                            ? t("settings.profile.usernameSaved")
                            : t("settings.profile.save")
                    }}
                </Button>
            </div>
            <p class="text-xs text-muted-foreground">{{ t("settings.profile.usernameHint") }}</p>
            <p v-if="usernameError" class="text-xs text-destructive">{{ usernameError }}</p>
        </div>

        <!-- Profile form -->
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
    </div>
</template>
