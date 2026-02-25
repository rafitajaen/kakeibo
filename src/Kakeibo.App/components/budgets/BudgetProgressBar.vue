<script setup lang="ts">
import { computed } from "vue";
import { Progress } from "@/components/ui/progress";

const props = defineProps<{
    currentSpending: number;
    limit: number;
}>();

// Clamp percentage between 0 and 100 for the progress bar display.
const percentage = computed(() => {
    if (props.limit <= 0) return 0;
    return Math.min(100, (props.currentSpending / props.limit) * 100);
});

// Color class applied to the progress indicator track.
const colorClass = computed(() => {
    if (props.limit <= 0) return "[&>div]:bg-green-500";
    const ratio = props.currentSpending / props.limit;
    if (ratio >= 1) return "[&>div]:bg-destructive";
    if (ratio >= 0.75) return "[&>div]:bg-yellow-500";
    return "[&>div]:bg-green-500";
});
</script>

<template>
    <Progress :model-value="percentage" :class="colorClass" />
</template>
