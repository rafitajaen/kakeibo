<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import RecurringList from "@/components/recurring/RecurringList.vue";
import ForecastList from "@/components/recurring/ForecastList.vue";
import { useRecurringStore } from "@/stores/recurring";

const { t } = useI18n();
const router = useRouter();
const recurringStore = useRecurringStore();

// Forecast window toggle — 30 or 90 days.
const forecastDays = ref(30);

onMounted(async () => {
    await Promise.all([
        recurringStore.fetchPatterns(),
        recurringStore.fetchForecast(forecastDays.value),
    ]);
});

async function handleDelete(id: string) {
    await recurringStore.deletePattern(id);
}

async function setForecastDays(days: number) {
    forecastDays.value = days;
    await recurringStore.fetchForecast(days);
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-3xl">
        <div class="flex items-center justify-between mb-6">
            <h1 class="text-2xl font-bold">{{ t("wallets.recurring.title") }}</h1>
            <Button @click="router.push({ name: 'recurring-create' })">
                {{ t("wallets.recurring.createNew") }}
            </Button>
        </div>

        <Tabs default-value="patterns">
            <TabsList class="mb-4">
                <TabsTrigger value="patterns">
                    {{ t("wallets.recurring.title") }}
                </TabsTrigger>
                <TabsTrigger value="forecast">
                    {{ t("wallets.recurring.forecast.title") }}
                </TabsTrigger>
            </TabsList>

            <!-- Patterns tab -->
            <TabsContent value="patterns">
                <div v-if="recurringStore.isLoading" class="text-center py-8 text-muted-foreground">
                    {{ t("common.loading") }}
                </div>

                <div
                    v-else-if="recurringStore.patterns.length === 0"
                    class="text-center py-12 text-muted-foreground"
                >
                    {{ t("wallets.recurring.empty") }}
                </div>

                <RecurringList v-else :patterns="recurringStore.patterns" @delete="handleDelete" />
            </TabsContent>

            <!-- Forecast tab -->
            <TabsContent value="forecast">
                <div class="flex gap-2 mb-4">
                    <Button
                        :variant="forecastDays === 30 ? 'default' : 'outline'"
                        size="sm"
                        @click="setForecastDays(30)"
                    >
                        {{ t("wallets.recurring.forecast.days30") }}
                    </Button>
                    <Button
                        :variant="forecastDays === 90 ? 'default' : 'outline'"
                        size="sm"
                        @click="setForecastDays(90)"
                    >
                        {{ t("wallets.recurring.forecast.days90") }}
                    </Button>
                </div>

                <div v-if="recurringStore.isLoading" class="text-center py-8 text-muted-foreground">
                    {{ t("common.loading") }}
                </div>

                <ForecastList v-else :items="recurringStore.forecast" />
            </TabsContent>
        </Tabs>
    </div>
</template>
