<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "@/stores/auth";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

const { t } = useI18n();
const route = useRoute();
const auth = useAuthStore();

type VerifyStatus = "pending" | "success" | "error";
const status = ref<VerifyStatus>("pending");

onMounted(async () => {
    const token = route.query.token as string | undefined;

    if (!token) {
        status.value = "error";
        return;
    }

    try {
        await auth.verifyEmail(token);
        status.value = "success";
    } catch {
        status.value = "error";
    }
});
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <Card>
                <CardHeader class="text-center">
                    <CardTitle>{{ t("auth.verifyEmail.title") }}</CardTitle>
                </CardHeader>

                <CardContent class="space-y-4 text-center">
                    <p v-if="status === 'pending'" class="text-sm text-muted-foreground">
                        {{ t("auth.verifyEmail.verifying") }}
                    </p>

                    <p v-if="status === 'success'" class="text-sm text-green-600">
                        {{ t("auth.verifyEmail.success") }}
                    </p>

                    <p v-if="status === 'error'" class="text-sm text-destructive">
                        {{ t("auth.verifyEmail.error") }}
                    </p>

                    <Button v-if="status !== 'pending'" variant="outline" as-child>
                        <router-link :to="{ name: 'login' }">
                            {{ t("auth.verifyEmail.backToLogin") }}
                        </router-link>
                    </Button>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
