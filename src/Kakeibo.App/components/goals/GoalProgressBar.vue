<script setup lang="ts">
const props = defineProps<{
    currentProgress: number;
    targetAmount: number;
}>();

// Clamp progress to 0–100%
const percentage = Math.min(100, Math.max(0, (props.currentProgress / props.targetAmount) * 100));

// Color reflects milestone status
const barColor =
    percentage >= 100
        ? "bg-green-500"
        : percentage >= 75
          ? "bg-blue-500"
          : percentage >= 50
            ? "bg-blue-400"
            : "bg-slate-400";
</script>

<template>
    <div class="w-full bg-secondary rounded-full h-2 overflow-hidden">
        <div
            :class="['h-2 rounded-full transition-all duration-300', barColor]"
            :style="{ width: `${percentage}%` }"
            role="progressbar"
            :aria-valuenow="Math.round(percentage)"
            aria-valuemin="0"
            aria-valuemax="100"
        />
    </div>
</template>
