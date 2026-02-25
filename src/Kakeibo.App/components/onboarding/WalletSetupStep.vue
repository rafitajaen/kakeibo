<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useWalletsStore } from "@/stores/wallets";

const { t } = useI18n();
const walletsStore = useWalletsStore();

const emit = defineEmits<{
    next: [];
    back: [];
    skip: [];
}>();

const isSubmitting = ref(false);
const errorMessage = ref("");

const schema = toTypedSchema(
    z.object({
        name: z.string().min(1).max(100),
    }),
);

const { handleSubmit, defineField, errors } = useForm({ validationSchema: schema });
const [name, nameAttrs] = defineField("name");

const onSubmit = handleSubmit(async (values) => {
    isSubmitting.value = true;
    errorMessage.value = "";
    try {
        await walletsStore.createWallet({ name: values.name, type: "Personal" });
        emit("next");
    } catch {
        errorMessage.value = t("common.errorGeneric");
    } finally {
        isSubmitting.value = false;
    }
});
</script>

<template>
    <div class="flex flex-col gap-6">
        <div class="text-center space-y-2">
            <h2 class="text-2xl font-bold">{{ t("onboarding.walletSetup.title") }}</h2>
            <p class="text-muted-foreground">{{ t("onboarding.walletSetup.description") }}</p>
        </div>
        <form class="space-y-4" @submit="onSubmit">
            <div class="space-y-2">
                <Label for="wallet-name">{{ t("onboarding.walletSetup.walletName") }}</Label>
                <Input
                    id="wallet-name"
                    v-model="name"
                    v-bind="nameAttrs"
                    :placeholder="t('onboarding.walletSetup.walletNamePlaceholder')"
                    :disabled="isSubmitting"
                />
                <p v-if="errors.name" class="text-sm text-destructive">{{ errors.name }}</p>
            </div>
            <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>
            <div class="flex gap-3">
                <Button type="button" variant="outline" class="flex-1" @click="emit('back')">
                    {{ t("common.back") }}
                </Button>
                <Button type="submit" class="flex-1" :disabled="isSubmitting">
                    {{ t("onboarding.walletSetup.create") }}
                </Button>
            </div>
        </form>
        <div class="text-center">
            <Button variant="ghost" size="sm" @click="emit('skip')">
                {{ t("onboarding.walletSetup.skip") }}
            </Button>
        </div>
    </div>
</template>
