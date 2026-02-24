<script setup lang="ts">
import { useI18n } from "vue-i18n";
import ForgotPasswordForm from "@/components/auth/ForgotPasswordForm.vue";
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";

const { t } = useI18n();
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-sm">
            <Card>
                <CardHeader class="text-center">
                    <CardTitle>{{ t("auth.forgotPassword.title") }}</CardTitle>
                    <CardDescription>{{ t("auth.forgotPassword.description") }}</CardDescription>
                </CardHeader>

                <CardContent>
                    <ForgotPasswordForm />
                </CardContent>

                <CardFooter class="justify-center text-sm">
                    <router-link
                        :to="{ name: 'login' }"
                        class="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
                    >
                        {{ t("auth.forgotPassword.backToLogin") }}
                    </router-link>
                </CardFooter>
            </Card>
        </div>
    </div>
</template>
