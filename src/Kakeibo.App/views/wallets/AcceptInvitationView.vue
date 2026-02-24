<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { useAuthStore } from "@/stores/auth";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();
const authStore = useAuthStore();

const code = route.params.code as string;
const isSubmitting = ref(false);
const accepted = ref(false);
const apiError = ref<string | null>(null);

onMounted(async () => {
    // Ensure auth state is resolved before checking.
    if (!authStore.initialized) {
        await authStore.fetchCurrentUser();
    }

    // Redirect unauthenticated users to login, preserving the invitation URL for post-login redirect.
    if (!authStore.isAuthenticated) {
        const redirect = route.fullPath;
        await router.replace({ name: "login", query: { redirect } });
    }
});

async function handleAccept() {
    isSubmitting.value = true;
    apiError.value = null;
    try {
        await walletsStore.acceptInvitation(code);
        accepted.value = true;
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
            apiError.value = t("wallets.invitation.errors.notFound");
        } else if (status === 422) {
            apiError.value = t("wallets.invitation.errors.validation");
        } else if (status === 409) {
            apiError.value = t("wallets.invitation.errors.conflict");
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
                <CardTitle>{{ t("wallets.invitation.acceptTitle") }}</CardTitle>
            </CardHeader>
            <CardContent class="space-y-4">
                <template v-if="accepted">
                    <p class="text-sm text-green-600">{{ t("wallets.invitation.accepted") }}</p>
                    <Button class="w-full" @click="router.push({ name: 'wallets' })">
                        {{ t("wallets.title") }}
                    </Button>
                </template>

                <template v-else>
                    <p class="text-sm text-muted-foreground">
                        {{ t("wallets.invitation.acceptDescription") }}
                    </p>
                    <p v-if="apiError" class="text-sm text-destructive">{{ apiError }}</p>
                    <Button class="w-full" :disabled="isSubmitting" @click="handleAccept">
                        {{ t("wallets.invitation.accept") }}
                    </Button>
                </template>
            </CardContent>
        </Card>
    </div>
</template>
