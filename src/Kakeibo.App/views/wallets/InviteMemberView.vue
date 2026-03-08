<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { getHttpStatus } from "@/lib/http";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import InvitationForm from "@/components/wallets/InvitationForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();

const walletId = route.params.id as string;
const isSubmitting = ref(false);
const successMessage = ref<string | null>(null);
const apiError = ref<string | null>(null);

async function handleSubmit(email: string) {
    isSubmitting.value = true;
    apiError.value = null;
    try {
        await walletsStore.inviteMember(walletId, email);
        successMessage.value = t("wallets.invitation.success");
    } catch (err: unknown) {
        const status = getHttpStatus(err);
        if (status === 409) {
            apiError.value = t("wallets.invitation.errors.conflict");
        } else if (status === 403) {
            apiError.value = t("wallets.members.errors.forbidden");
        } else {
            apiError.value = t("wallets.invitation.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="container mx-auto max-w-md px-4 py-8">
        <Card>
            <CardHeader>
                <CardTitle>{{ t("wallets.invitation.title") }}</CardTitle>
            </CardHeader>
            <CardContent>
                <p v-if="successMessage" class="mb-4 text-sm text-green-600">
                    {{ successMessage }}
                </p>
                <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

                <InvitationForm :is-submitting="isSubmitting" @submit="handleSubmit" />

                <Button
                    variant="ghost"
                    class="mt-4 w-full"
                    @click="router.push({ name: 'wallet-members', params: { id: walletId } })"
                >
                    {{ t("common.cancel") }}
                </Button>
            </CardContent>
        </Card>
    </div>
</template>
