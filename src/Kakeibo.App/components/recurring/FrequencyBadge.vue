<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { Badge } from "@/components/ui/badge";

const props = defineProps<{
    frequency: string;
}>();

const { t } = useI18n();

// Map each frequency to a Badge variant for visual distinction.
const variant = computed(() => {
    switch (props.frequency.toLowerCase()) {
        case "daily":
            return "destructive";
        case "weekly":
            return "default";
        case "biweekly":
            return "secondary";
        case "monthly":
            return "outline";
        case "yearly":
            return "secondary";
        default:
            return "outline";
    }
});

const label = computed(() => t(`wallets.recurring.frequency.${props.frequency.toLowerCase()}`));
</script>

<template>
    <Badge :variant="variant">{{ label }}</Badge>
</template>
