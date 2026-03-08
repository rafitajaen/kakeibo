<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import GoalList from "@/components/goals/GoalList.vue";
import { useGoalsStore } from "@/stores/goals";

const { t } = useI18n();
const router = useRouter();
const goalsStore = useGoalsStore();

onMounted(async () => {
    await goalsStore.fetchGoals();
});

async function handleDelete(id: string) {
    await goalsStore.deleteGoal(id);
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-3xl">
        <div class="flex items-center justify-between mb-6">
            <h1 class="text-2xl font-bold">{{ t("wallets.goals.title") }}</h1>
            <Button @click="router.push({ name: 'goal-create' })">
                {{ t("wallets.goals.createNew") }}
            </Button>
        </div>

        <div v-if="goalsStore.isLoading" class="space-y-4">
            <Skeleton v-for="i in 3" :key="i" class="h-28 w-full rounded-lg" />
        </div>

        <div
            v-else-if="goalsStore.goals.length === 0"
            class="text-center py-12 text-muted-foreground"
        >
            {{ t("wallets.goals.empty") }}
        </div>

        <GoalList v-else :goals="goalsStore.goals" @delete="handleDelete" />
    </div>
</template>
