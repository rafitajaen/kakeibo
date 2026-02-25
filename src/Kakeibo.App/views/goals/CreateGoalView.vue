<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useGoalsStore } from "@/stores/goals";
import type { CreateGoalData } from "@/stores/goals";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import GoalForm from "@/components/goals/GoalForm.vue";

const { t } = useI18n();
const router = useRouter();
const goalsStore = useGoalsStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await walletsStore.fetchWallets();
    } catch {
        apiError.value = t("wallets.goals.errors.unexpected");
    }
});

async function handleSubmit(values: CreateGoalData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await goalsStore.createGoal(values);
        await router.push({ name: "goals" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 422) {
            apiError.value = t("wallets.goals.errors.validation");
        } else {
            apiError.value = t("wallets.goals.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}
</script>

<template>
    <div class="flex min-h-screen items-center justify-center bg-background px-4">
        <div class="w-full max-w-md">
            <Card>
                <CardHeader>
                    <CardTitle>{{ t("wallets.goals.form.submit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <GoalForm
                        :wallets="walletsStore.activeWallets"
                        :is-submitting="isSubmitting"
                        @submit="handleSubmit"
                    />
                    <p v-if="apiError" class="mt-2 text-sm text-destructive">{{ apiError }}</p>
                </CardContent>
            </Card>
        </div>
    </div>
</template>
