<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import ActivityItem from "@/components/activity/ActivityItem.vue";
import type { ActivityEntry, ActivityFilters } from "@/stores/activity";

const props = defineProps<{
    activities: ActivityEntry[];
    total: number;
    filters: ActivityFilters;
    isLoading: boolean;
}>();

const emit = defineEmits<{
    (e: "page-change", page: number): void;
}>();

const { t } = useI18n();

const totalPages = () => Math.ceil(props.total / props.filters.pageSize);

function goToPreviousPage() {
    if (props.filters.page > 1) {
        emit("page-change", props.filters.page - 1);
    }
}

function goToNextPage() {
    if (props.filters.page < totalPages()) {
        emit("page-change", props.filters.page + 1);
    }
}
</script>

<template>
    <div class="space-y-4">
        <!-- Loading skeleton -->
        <div v-if="isLoading" class="space-y-2">
            <div v-for="i in 5" :key="i" class="h-12 bg-muted animate-pulse rounded" />
        </div>

        <!-- Empty state -->
        <div
            v-else-if="activities.length === 0"
            class="py-8 text-center text-muted-foreground"
            data-testid="activity-empty"
        >
            {{ t("activity.noActivity") }}
        </div>

        <!-- Activity items -->
        <div v-else class="border rounded-lg divide-y" data-testid="activity-feed">
            <ActivityItem v-for="entry in activities" :key="entry.id" :entry="entry" />
        </div>

        <!-- Pagination -->
        <div v-if="total > filters.pageSize" class="flex items-center justify-between">
            <Button
                variant="outline"
                size="sm"
                :disabled="filters.page <= 1"
                @click="goToPreviousPage"
            >
                {{ t("activity.pagination.previous") }}
            </Button>
            <span class="text-sm text-muted-foreground">
                {{ filters.page }} / {{ totalPages() }}
            </span>
            <Button
                variant="outline"
                size="sm"
                :disabled="filters.page >= totalPages()"
                @click="goToNextPage"
            >
                {{ t("activity.pagination.next") }}
            </Button>
        </div>
    </div>
</template>
