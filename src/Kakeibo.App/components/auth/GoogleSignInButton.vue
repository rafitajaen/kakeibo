<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "@/stores/auth";
import { Button } from "@/components/ui/button";

const props = defineProps<{
    label: string;
    currency?: string;
}>();

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const loading = ref(false);
const error = ref<string | null>(null);

declare const google: {
    accounts: {
        id: {
            initialize: (config: {
                client_id: string;
                callback: (response: { credential: string }) => void;
            }) => void;
            prompt: () => void;
        };
    };
};

async function handleGoogleSignIn() {
    loading.value = true;
    error.value = null;

    try {
        const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
        if (!clientId) {
            error.value = t("auth.errors.googleFailed");
            return;
        }

        // Load Google Identity Services script if not already loaded
        if (typeof google === "undefined" || !google?.accounts?.id) {
            await loadGoogleScript();
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: async (response: { credential: string }) => {
                try {
                    await auth.loginWithGoogle(response.credential, props.currency || "EUR");
                    await router.push({ name: "home" });
                } catch {
                    error.value = t("auth.errors.googleFailed");
                } finally {
                    loading.value = false;
                }
            },
        });

        google.accounts.id.prompt();
    } catch {
        error.value = t("auth.errors.googleFailed");
        loading.value = false;
    }
}

function loadGoogleScript(): Promise<void> {
    return new Promise((resolve, reject) => {
        if (document.querySelector('script[src*="accounts.google.com/gsi/client"]')) {
            resolve();
            return;
        }
        const script = document.createElement("script");
        script.src = "https://accounts.google.com/gsi/client";
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load Google script"));
        document.head.appendChild(script);
    });
}
</script>

<template>
    <div>
        <Button
            type="button"
            variant="outline"
            class="w-full"
            :disabled="loading"
            @click="handleGoogleSignIn"
        >
            <svg class="mr-2 h-4 w-4" viewBox="0 0 24 24">
                <path
                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"
                    fill="#4285F4"
                />
                <path
                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                    fill="#34A853"
                />
                <path
                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                    fill="#FBBC05"
                />
                <path
                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                    fill="#EA4335"
                />
            </svg>
            {{ label }}
        </Button>
        <p v-if="error" class="mt-2 text-sm text-destructive">{{ error }}</p>
    </div>
</template>
