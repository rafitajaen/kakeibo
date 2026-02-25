<script setup lang="ts">
defineProps<{
    currentStep: number;
    totalSteps: number;
}>();
</script>

<template>
    <div
        class="flex items-center justify-center gap-2"
        role="progressbar"
        :aria-valuenow="currentStep + 1"
        :aria-valuemax="totalSteps"
    >
        <div
            v-for="i in totalSteps"
            :key="i"
            class="w-2 h-2 rounded-full transition-all duration-300"
            :class="i - 1 === currentStep ? 'bg-primary w-4' : 'bg-muted'"
        />
    </div>
</template>
