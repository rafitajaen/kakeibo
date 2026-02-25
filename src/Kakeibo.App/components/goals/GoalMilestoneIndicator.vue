<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
    lastMilestone: number;
    targetAmount: number;
    currentProgress: number;
}>();

const { t } = useI18n();

const milestones = [25, 50, 75, 100] as const;

// A milestone dot is "reached" if the lastMilestone field has recorded it.
const isReached = computed(() => (milestone: number) => props.lastMilestone >= milestone);

// Label for each milestone threshold.
function milestoneLabel(milestone: number): string {
    const key = `wallets.goals.milestones.${milestone}`;
    return t(key);
}
</script>

<template>
    <div class="flex items-center gap-3">
        <div
            v-for="milestone in milestones"
            :key="milestone"
            class="flex flex-col items-center gap-1"
            :title="milestoneLabel(milestone)"
        >
            <div
                :class="[
                    'h-3 w-3 rounded-full transition-colors',
                    isReached(milestone)
                        ? 'bg-green-500 dark:bg-green-400'
                        : 'bg-gray-200 dark:bg-gray-700',
                ]"
            />
            <span class="text-xs text-muted-foreground">{{ milestone }}%</span>
        </div>
    </div>
</template>
