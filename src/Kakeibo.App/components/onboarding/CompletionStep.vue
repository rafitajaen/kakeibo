<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";

const { t } = useI18n();

const emit = defineEmits<{
    finish: [];
}>();
</script>

<template>
    <div class="flex flex-col items-center text-center gap-6 py-8">
        <div class="text-6xl">🎉</div>
        <div class="space-y-2">
            <h2 class="text-3xl font-bold">{{ t("onboarding.completion.title") }}</h2>
            <p class="text-muted-foreground">{{ t("onboarding.completion.description") }}</p>
        </div>
        <Button size="lg" @click="emit('finish')">
            {{ t("onboarding.completion.goToDashboard") }}
        </Button>
    </div>
</template>
