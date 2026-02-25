<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { HugeiconsIcon } from "@hugeicons/vue";
import {
    Wallet01Icon,
    MoneyExchange01Icon,
    ChartBreakoutCircleIcon,
    Target01Icon,
} from "@hugeicons/core-free-icons";

const { t } = useI18n();

const emit = defineEmits<{
    next: [];
    back: [];
}>();

const features = [
    { icon: Wallet01Icon, key: "wallets" },
    { icon: MoneyExchange01Icon, key: "transactions" },
    { icon: ChartBreakoutCircleIcon, key: "budgets" },
    { icon: Target01Icon, key: "goals" },
];
</script>

<template>
    <div class="flex flex-col gap-6">
        <div class="text-center space-y-2">
            <h2 class="text-2xl font-bold">{{ t("onboarding.tour.title") }}</h2>
        </div>
        <div class="grid grid-cols-2 gap-3">
            <Card v-for="feature in features" :key="feature.key" class="text-center">
                <CardHeader class="pb-2">
                    <div class="flex justify-center mb-2">
                        <HugeiconsIcon :icon="feature.icon" class="w-8 h-8 text-primary" />
                    </div>
                    <CardTitle class="text-sm">
                        {{ t(`onboarding.tour.${feature.key}.title`) }}
                    </CardTitle>
                </CardHeader>
                <CardContent class="pt-0">
                    <p class="text-xs text-muted-foreground">
                        {{ t(`onboarding.tour.${feature.key}.description`) }}
                    </p>
                </CardContent>
            </Card>
        </div>
        <div class="flex gap-3">
            <Button variant="outline" class="flex-1" @click="emit('back')">
                {{ t("common.back") }}
            </Button>
            <Button class="flex-1" @click="emit('next')">
                {{ t("common.next") }}
            </Button>
        </div>
    </div>
</template>
