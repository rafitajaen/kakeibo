<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import ActivityFilters from "@/components/activity/ActivityFilters.vue";
import ActivityFeed from "@/components/activity/ActivityFeed.vue";
import { useActivityStore } from "@/stores/activity";
import type { ActivityFilters as IActivityFilters } from "@/stores/activity";

const { t } = useI18n();
const store = useActivityStore();

onMounted(async () => {
    await store.fetchActivities();
});

async function applyFilters(filters: Partial<IActivityFilters>) {
    await store.fetchActivities(filters);
}

async function changePage(page: number) {
    await store.fetchActivities({ page });
}
</script>

<template>
    <div class="container max-w-3xl py-6 space-y-6">
        <h1 class="text-2xl font-bold">{{ t("activity.title") }}</h1>

        <ActivityFilters @apply="applyFilters" />

        <ActivityFeed
            :activities="store.activities"
            :total="store.total"
            :filters="store.filters"
            :is-loading="store.isLoading"
            @page-change="changePage"
        />
    </div>
</template>
