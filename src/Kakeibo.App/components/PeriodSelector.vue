<script setup lang="ts">
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import {
    format,
    startOfDay,
    startOfWeek,
    endOfWeek,
    startOfMonth,
    endOfMonth,
    startOfYear,
    endOfYear,
    addDays,
    addWeeks,
    addMonths,
    addYears,
    subDays,
    subWeeks,
    subMonths,
    subYears,
} from "date-fns";
import { ChevronLeft, ChevronRight } from "lucide-vue-next";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";

export type PeriodType = "always" | "today" | "week" | "month" | "year";

export interface PeriodValue {
    type: PeriodType;
    from?: string;
    to?: string;
    // Anchor date used for navigation (ISO date string, e.g. "2026-03-01")
    anchor?: string;
}

const props = withDefaults(
    defineProps<{
        modelValue: PeriodValue;
    }>(),
    {
        modelValue: () => ({ type: "month" }),
    },
);

const emit = defineEmits<{
    (e: "update:modelValue", value: PeriodValue): void;
}>();

const { t } = useI18n();
const sheetOpen = ref(false);

const PERIOD_OPTIONS: PeriodType[] = ["always", "today", "week", "month", "year"];

// Returns a human-readable label for the current period value.
const label = computed(() => {
    const { type, anchor } = props.modelValue;
    const base = anchor ? new Date(anchor) : new Date();

    if (type === "always") return t("period.always");
    if (type === "today") return format(base, "d MMM yyyy");
    if (type === "week") {
        const s = startOfWeek(base, { weekStartsOn: 1 });
        const e = endOfWeek(base, { weekStartsOn: 1 });
        return `${format(s, "d MMM")} – ${format(e, "d MMM yyyy")}`;
    }
    if (type === "month") return format(base, "MMMM yyyy");
    if (type === "year") return format(base, "yyyy");
    return "";
});

// Derives from/to strings from anchor + type.
function derivePeriod(type: PeriodType, anchor: Date): { from?: string; to?: string } {
    const iso = (d: Date) => format(d, "yyyy-MM-dd");
    if (type === "always") return {};
    if (type === "today") return { from: iso(startOfDay(anchor)), to: iso(startOfDay(anchor)) };
    if (type === "week")
        return {
            from: iso(startOfWeek(anchor, { weekStartsOn: 1 })),
            to: iso(endOfWeek(anchor, { weekStartsOn: 1 })),
        };
    if (type === "month") return { from: iso(startOfMonth(anchor)), to: iso(endOfMonth(anchor)) };
    if (type === "year") return { from: iso(startOfYear(anchor)), to: iso(endOfYear(anchor)) };
    return {};
}

function navigate(direction: "prev" | "next") {
    const { type, anchor } = props.modelValue;
    if (type === "always") return;

    const base = anchor ? new Date(anchor) : new Date();
    const d = direction === "prev" ? -1 : 1;
    let newAnchor: Date;

    if (type === "today") newAnchor = d > 0 ? addDays(base, 1) : subDays(base, 1);
    else if (type === "week") newAnchor = d > 0 ? addWeeks(base, 1) : subWeeks(base, 1);
    else if (type === "month") newAnchor = d > 0 ? addMonths(base, 1) : subMonths(base, 1);
    else if (type === "year") newAnchor = d > 0 ? addYears(base, 1) : subYears(base, 1);
    else return;

    const { from, to } = derivePeriod(type, newAnchor);
    emit("update:modelValue", {
        type,
        anchor: format(newAnchor, "yyyy-MM-dd"),
        from,
        to,
    });
}

function selectType(type: PeriodType) {
    sheetOpen.value = false;
    const anchor = new Date();
    const { from, to } = derivePeriod(type, anchor);
    emit("update:modelValue", {
        type,
        anchor: format(anchor, "yyyy-MM-dd"),
        from,
        to,
    });
}
</script>

<template>
    <div class="flex items-center gap-1 px-4 py-2">
        <button
            class="flex size-8 items-center justify-center rounded-full text-muted-foreground hover:bg-muted disabled:opacity-30"
            :disabled="modelValue.type === 'always'"
            @click="navigate('prev')"
            :aria-label="t('period.previous')"
        >
            <ChevronLeft class="size-4" />
        </button>

        <button
            class="flex flex-1 items-center justify-center gap-1 rounded-full bg-muted px-4 py-1.5 text-sm font-medium"
            @click="sheetOpen = true"
        >
            <span>{{ label }}</span>
        </button>

        <button
            class="flex size-8 items-center justify-center rounded-full text-muted-foreground hover:bg-muted disabled:opacity-30"
            :disabled="modelValue.type === 'always'"
            @click="navigate('next')"
            :aria-label="t('period.next')"
        >
            <ChevronRight class="size-4" />
        </button>
    </div>

    <Sheet v-model:open="sheetOpen">
        <SheetContent side="bottom" class="rounded-t-2xl pb-safe">
            <SheetHeader class="mb-4">
                <SheetTitle>{{ t("period.select") }}</SheetTitle>
            </SheetHeader>
            <div class="grid grid-cols-3 gap-3 pb-4">
                <button
                    v-for="opt in PERIOD_OPTIONS"
                    :key="opt"
                    class="rounded-xl border py-4 text-sm font-medium transition-colors"
                    :class="
                        modelValue.type === opt
                            ? 'border-primary bg-primary text-primary-foreground'
                            : 'border-border bg-muted/40 text-foreground hover:bg-muted'
                    "
                    @click="selectType(opt)"
                >
                    {{ t(`period.${opt}`) }}
                </button>
            </div>
        </SheetContent>
    </Sheet>
</template>
