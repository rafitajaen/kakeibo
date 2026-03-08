<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { getHttpStatus } from "@/lib/http";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import WalletForm from "@/components/wallets/WalletForm.vue";

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

async function handleSubmit(values: { name: string; type: string }) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await walletsStore.createWallet(values);
        await router.push({ name: "wallets" });
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 409) {
            apiError.value = t("wallets.errors.conflict");
        } else {
            apiError.value = t("wallets.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <Card>
                <CardHeader>
                    <CardTitle>{{ t("wallets.form.submit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <WalletForm :is-submitting="isSubmitting" @submit="handleSubmit" />
                    <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
