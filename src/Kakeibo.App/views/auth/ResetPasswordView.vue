<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import ResetPasswordForm from "@/components/auth/ResetPasswordForm.vue";
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";

const { t } = useI18n();
const route = useRoute();

// Extract the reset token from the URL — links arrive as /reset-password?token=<value>
const token = computed(() => (route.query.token as string) ?? "");
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <Card>
                <CardHeader class="text-center">
                    <CardTitle>{{ t("auth.resetPassword.title") }}</CardTitle>
                    <CardDescription>{{ t("auth.resetPassword.description") }}</CardDescription>
                </CardHeader>

                <CardContent>
                    <ResetPasswordForm v-if="token" :token="token" />
                    <p v-else class="text-sm text-destructive">
                        {{ t("auth.errors.tokenInvalid") }}
                    </p>
                </CardContent>

                <CardFooter class="justify-center text-sm">
                    <router-link
                        :to="{ name: 'login' }"
                        class="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
                    >
                        {{ t("auth.resetPassword.backToLogin") }}
                    </router-link>
                </CardFooter>
            </Card>
        </div>
    </div>
</template>
