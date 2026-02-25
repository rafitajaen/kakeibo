<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useGoalsStore } from "@/stores/goals";
import type { UpdateGoalData } from "@/stores/goals";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import GoalForm from "@/components/goals/GoalForm.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const goalsStore = useGoalsStore();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);
const isSubmitting = ref(false);

onMounted(async () => {
    try {
        await Promise.all([
            goalsStore.goals.length === 0 ? goalsStore.fetchGoals() : Promise.resolve(),
            walletsStore.fetchWallets(),
        ]);
    } catch {
        apiError.value = t("wallets.goals.errors.unexpected");
    }
});

const goal = computed(() => goalsStore.goals.find((g) => g.id === (route.params.id as string)));

async function handleSubmit(values: UpdateGoalData) {
    apiError.value = null;
    isSubmitting.value = true;
    try {
        await goalsStore.updateGoal(route.params.id as string, values);
        await router.push({ name: "goals" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 422) {
            apiError.value = t("wallets.goals.errors.validation");
        } else if (status === 403) {
            apiError.value = t("wallets.goals.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("wallets.goals.errors.notFound");
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
            <p v-if="apiError" class="mb-4 text-sm text-destructive">{{ apiError }}</p>

            <Card v-if="goal">
                <CardHeader>
                    <CardTitle>{{ t("wallets.goals.form.submitEdit") }}</CardTitle>
                </CardHeader>
                <CardContent>
                    <GoalForm
                        :goal="goal"
                        :wallets="walletsStore.activeWallets"
                        :is-submitting="isSubmitting"
                        @submit="handleSubmit"
                    />
                </CardContent>
            </Card>

            <p v-else-if="!apiError" class="text-muted-foreground">{{ t("common.loading") }}</p>
        </div>
    </div>
</template>
