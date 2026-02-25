<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { Badge } from "@/components/ui/badge";

const props = defineProps<{
    currentSpending: number;
    limit: number;
}>();

const { t } = useI18n();

// Derive status from spending ratio (same thresholds as backend).
const status = computed(() => {
    if (props.limit <= 0) return "onTrack";
    const ratio = props.currentSpending / props.limit;
    if (ratio >= 1) return "exceeded";
    if (ratio >= 0.75) return "warning";
    return "onTrack";
});

const variant = computed(() => {
    if (status.value === "exceeded") return "destructive";
    if (status.value === "warning") return "secondary";
    return "default";
});

const label = computed(() => t(`wallets.budgets.status.${status.value}`));
</script>

<template>
    <Badge :variant="variant">{{ label }}</Badge>
</template>
