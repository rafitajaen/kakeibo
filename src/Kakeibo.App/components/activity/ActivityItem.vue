<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { formatDistanceToNow, parseISO } from "date-fns";
import type { ActivityEntry } from "@/stores/activity";

defineProps<{
    entry: ActivityEntry;
}>();

const { t } = useI18n();

// Returns a human-readable action label, falling back to the raw action key.
function actionLabel(action: string): string {
    const key = `activity.actions.${action}`;
    const translated = t(key);
    // vue-i18n returns the key when no translation is found
    return translated === key ? action : translated;
}

function relativeTime(isoString: string): string {
    try {
        return formatDistanceToNow(parseISO(isoString), { addSuffix: true });
    } catch {
        return isoString;
    }
}
</script>

<template>
    <div class="flex items-start gap-3 py-3 border-b last:border-b-0" data-testid="activity-item">
        <!-- Action dot -->
        <div class="mt-1.5 h-2 w-2 rounded-full bg-primary flex-shrink-0" />

        <div class="flex-1 min-w-0">
            <p class="text-sm font-medium">{{ actionLabel(entry.action) }}</p>
            <p class="text-xs text-muted-foreground mt-0.5">
                {{ relativeTime(entry.occurredAt) }}
            </p>
        </div>
    </div>
</template>
