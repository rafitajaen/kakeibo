<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import WalletForm from "@/components/wallets/WalletForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await walletsStore.fetchWallet(route.params.id as string);
    } catch {
        apiError.value = t("wallets.errors.notFound");
    }
});

async function handleSubmit(values: { name: string; type: string }) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await walletsStore.updateWallet(route.params.id as string, { name: values.name });
        await router.push({ name: "wallet-detail", params: { id: route.params.id } });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            apiError.value = t("wallets.errors.conflict");
        } else if (status === 403) {
            apiError.value = t("wallets.errors.forbidden");
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
                    <CardTitle>{{ t("wallets.actions.edit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <template v-if="walletsStore.currentWallet">
                        <WalletForm
                            :initial-name="walletsStore.currentWallet.name"
                            :is-submitting="isSubmitting"
                            @submit="handleSubmit"
                        />
                        <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                    </template>
                    <p v-else class="text-muted-foreground">{{ t("common.loading") }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
