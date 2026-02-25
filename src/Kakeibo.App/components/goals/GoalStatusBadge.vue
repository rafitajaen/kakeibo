<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
    currentProgress: number;
    targetAmount: number;
}>();

const { t } = useI18n();

const percentage = computed(() =>
    props.targetAmount > 0 ? (props.currentProgress / props.targetAmount) * 100 : 0,
);

const status = computed(() => {
    if (percentage.value >= 100) return "achieved";
    if (percentage.value >= 75) return "nearTarget";
    if (percentage.value > 0) return "inProgress";
    return "notStarted";
});

const label = computed(() => t(`wallets.goals.status.${status.value}`));

const badgeClass = computed(() => {
    switch (status.value) {
        case "achieved":
            return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
        case "nearTarget":
            return "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200";
        case "inProgress":
            return "bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-200";
        default:
            return "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400";
    }
});
</script>

<template>
    <span :class="['inline-flex items-center px-2 py-0.5 rounded text-xs font-medium', badgeClass]">
        {{ label }}
    </span>
</template>
