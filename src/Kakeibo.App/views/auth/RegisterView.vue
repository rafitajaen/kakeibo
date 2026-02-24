<script setup lang="ts">
import { useI18n } from "vue-i18n";
import RegisterForm from "@/components/auth/RegisterForm.vue";
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
                    <CardTitle>{{ t("auth.register.title") }}</CardTitle>
                    <CardDescription>{{ t("auth.register.description") }}</CardDescription>
                </CardHeader>

                <CardContent>
                    <RegisterForm />
                </CardContent>

                <CardFooter class="justify-center text-sm">
                    <span class="text-muted-foreground">
                        {{ t("auth.register.hasAccount") }}
                        <router-link
                            :to="{ name: 'login' }"
                            class="text-foreground font-medium underline-offset-4 hover:underline"
                        >
                            {{ t("auth.register.login") }}
                        </router-link>
                    </span>
                </CardFooter>
            </Card>
        </div>
    </div>
</template>
