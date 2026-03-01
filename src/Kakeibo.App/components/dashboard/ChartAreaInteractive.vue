<script setup lang="ts">
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { subDays, format } from "date-fns";
import { VisXYContainer, VisArea, VisLine, VisAxis, VisCrosshair, VisTooltip } from "@unovis/vue";
import type { DailyData } from "@/composables/useDashboardData";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";

const props = defineProps<{
    data: DailyData[];
}>();

const { t } = useI18n();

type Range = "7d" | "30d" | "90d";
const range = ref<Range>("30d");

const rangeDays: Record<Range, number> = { "7d": 7, "30d": 30, "90d": 90 };

const filteredData = computed(() => {
    const cutoff = subDays(new Date(), rangeDays[range.value]);
    return props.data.filter((d) => d.date >= cutoff);
});

// Unovis accessors
const xAccessor = (d: DailyData) => d.date.getTime();
const incomeAccessor = (d: DailyData) => d.income;
const expensesAccessor = (d: DailyData) => d.expenses;

const xTickFormat = (ms: number) => format(new Date(ms), range.value === "7d" ? "EEE" : "MMM d");

// Crosshair tooltip template
function tooltipTemplate(d: DailyData) {
    return `
        <div class="rounded-lg border bg-background p-2 shadow-md text-xs min-w-[120px]">
            <p class="font-medium mb-1">${format(d.date, "MMM d, yyyy")}</p>
            <p class="text-green-600">${t("dashboard.chart.income")}: ${d.income.toFixed(2)}</p>
            <p class="text-red-600">${t("dashboard.chart.expenses")}: ${d.expenses.toFixed(2)}</p>
        </div>
    `;
}
</script>

<template>
    <Card>
        <CardHeader class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
                <CardTitle>{{ t("dashboard.chart.title") }}</CardTitle>
                <CardDescription>{{ t("dashboard.chart.description") }}</CardDescription>
            </div>

            <!-- Desktop toggle -->
            <ToggleGroup v-model="range" type="single" class="hidden sm:flex">
                <ToggleGroupItem value="90d" size="sm">
                    {{ t("dashboard.chart.range90d") }}
                </ToggleGroupItem>
                <ToggleGroupItem value="30d" size="sm">
                    {{ t("dashboard.chart.range30d") }}
                </ToggleGroupItem>
                <ToggleGroupItem value="7d" size="sm">
                    {{ t("dashboard.chart.range7d") }}
                </ToggleGroupItem>
            </ToggleGroup>

            <!-- Mobile select -->
            <Select v-model="range" class="sm:hidden w-[160px]">
                <SelectTrigger>
                    <SelectValue />
                </SelectTrigger>
                <SelectContent>
                    <SelectItem value="7d">{{ t("dashboard.chart.range7d") }}</SelectItem>
                    <SelectItem value="30d">{{ t("dashboard.chart.range30d") }}</SelectItem>
                    <SelectItem value="90d">{{ t("dashboard.chart.range90d") }}</SelectItem>
                </SelectContent>
            </Select>
        </CardHeader>

        <CardContent class="px-2 sm:px-6">
            <VisXYContainer :data="filteredData" :height="220" class="w-full">
                <VisArea :x="xAccessor" :y="incomeAccessor" color="var(--chart-2)" :opacity="0.3" />
                <VisLine :x="xAccessor" :y="incomeAccessor" color="var(--chart-2)" />
                <VisArea
                    :x="xAccessor"
                    :y="expensesAccessor"
                    color="var(--chart-1)"
                    :opacity="0.3"
                />
                <VisLine :x="xAccessor" :y="expensesAccessor" color="var(--chart-1)" />
                <VisAxis type="x" :tick-format="xTickFormat" />
                <VisCrosshair :template="tooltipTemplate" />
                <VisTooltip />
            </VisXYContainer>

            <!-- Legend -->
            <div class="mt-2 flex items-center justify-center gap-6 text-xs text-muted-foreground">
                <span class="flex items-center gap-1">
                    <span class="inline-block size-2 rounded-full bg-[var(--chart-2)]" />
                    {{ t("dashboard.chart.income") }}
                </span>
                <span class="flex items-center gap-1">
                    <span class="inline-block size-2 rounded-full bg-[var(--chart-1)]" />
                    {{ t("dashboard.chart.expenses") }}
                </span>
            </div>
        </CardContent>
    </Card>
</template>
