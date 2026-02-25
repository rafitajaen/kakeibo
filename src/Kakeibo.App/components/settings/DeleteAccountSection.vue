<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { useSettingsStore } from "@/stores/settings";
import { useAuthStore } from "@/stores/auth";

const { t } = useI18n();
const router = useRouter();
const settingsStore = useSettingsStore();
const authStore = useAuthStore();

const password = ref("");
const isDeleting = ref(false);
const errorMessage = ref("");

async function confirmDelete() {
    if (!password.value) return;
    isDeleting.value = true;
    errorMessage.value = "";
    try {
        await settingsStore.deleteAccount(password.value);
        authStore.clearUser();
        router.replace({ name: "login" });
    } catch {
        errorMessage.value = t("common.errorGeneric");
    } finally {
        isDeleting.value = false;
        password.value = "";
    }
}
</script>

<template>
    <div class="space-y-4 rounded-lg border border-destructive/50 p-4">
        <div>
            <h3 class="text-base font-semibold text-destructive">
                {{ t("settings.deleteAccount.title") }}
            </h3>
            <p class="text-sm text-muted-foreground mt-1">
                {{ t("settings.deleteAccount.description") }}
            </p>
        </div>
        <p class="text-sm text-amber-600 dark:text-amber-400">
            {{ t("settings.deleteAccount.warning") }}
        </p>
        <AlertDialog>
            <AlertDialogTrigger as-child>
                <Button variant="destructive">{{ t("settings.deleteAccount.confirm") }}</Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>{{ t("settings.deleteAccount.title") }}</AlertDialogTitle>
                    <AlertDialogDescription>
                        {{ t("settings.deleteAccount.description") }}
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <div class="space-y-2 py-2">
                    <Label for="delete-password">{{ t("settings.deleteAccount.password") }}</Label>
                    <Input
                        id="delete-password"
                        v-model="password"
                        type="password"
                        :placeholder="t('settings.deleteAccount.passwordPlaceholder')"
                        :disabled="isDeleting"
                    />
                    <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>
                </div>
                <AlertDialogFooter>
                    <AlertDialogCancel @click="password = ''">{{
                        t("common.cancel")
                    }}</AlertDialogCancel>
                    <AlertDialogAction
                        class="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                        :disabled="!password || isDeleting"
                        @click.prevent="confirmDelete"
                    >
                        {{ t("settings.deleteAccount.confirm") }}
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    </div>
</template>
