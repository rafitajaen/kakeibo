<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";

const { t } = useI18n();

const emit = defineEmits<{
    next: [];
    skip: [];
}>();
</script>

<template>
    <div class="flex flex-col items-center text-center gap-6 py-8">
        <div class="text-6xl">🏮</div>
        <div class="space-y-2">
            <h2 class="text-3xl font-bold">{{ t("onboarding.welcome.title") }}</h2>
            <p class="text-lg text-muted-foreground">{{ t("onboarding.welcome.subtitle") }}</p>
        </div>
        <p class="text-muted-foreground max-w-sm">{{ t("onboarding.welcome.description") }}</p>
        <div class="flex flex-col sm:flex-row gap-3 w-full max-w-xs">
            <Button class="flex-1" @click="emit('next')">
                {{ t("onboarding.welcome.getStarted") }}
            </Button>
            <Button variant="ghost" @click="emit('skip')">
                {{ t("onboarding.welcome.skip") }}
            </Button>
        </div>
    </div>
</template>
