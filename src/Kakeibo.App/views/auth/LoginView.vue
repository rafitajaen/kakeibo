<script setup lang="ts">
import { useI18n } from "vue-i18n";
import LoginForm from "@/components/auth/LoginForm.vue";
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
                    <CardTitle>{{ t("auth.login.title") }}</CardTitle>
                    <CardDescription>{{ t("auth.login.description") }}</CardDescription>
                </CardHeader>

                <CardContent>
                    <LoginForm />
                </CardContent>

                <CardFooter class="flex justify-between text-sm">
                    <router-link
                        :to="{ name: 'forgot-password' }"
                        class="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
                    >
                        {{ t("auth.login.forgotPassword") }}
                    </router-link>
                    <span class="text-muted-foreground">
                        {{ t("auth.login.noAccount") }}
                        <router-link
                            :to="{ name: 'register' }"
                            class="text-foreground font-medium underline-offset-4 hover:underline"
                        >
                            {{ t("auth.login.register") }}
                        </router-link>
                    </span>
                </CardFooter>
            </Card>
        </div>
    </div>
</template>
