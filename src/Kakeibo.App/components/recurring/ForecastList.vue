<script setup lang="ts">
import { useI18n } from "vue-i18n";
import UpcomingTransactionCard from "@/components/recurring/UpcomingTransactionCard.vue";
import type { ForecastItem } from "@/stores/recurring";

defineProps<{
    items: ForecastItem[];
}>();

const { t } = useI18n();
</script>

<template>
    <div class="space-y-2">
        <div v-if="items.length === 0" class="text-center py-8 text-muted-foreground">
            {{ t("wallets.recurring.forecast.empty") }}
        </div>

        <UpcomingTransactionCard
            v-for="(item, index) in items"
            :key="`${item.patternId}-${item.date}-${index}`"
            :item="item"
        />
    </div>
</template>
