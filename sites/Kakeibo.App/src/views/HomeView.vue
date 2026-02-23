<script setup lang="ts">
import { useI18n } from "vue-i18n";

const { t } = useI18n();
</script>

<template>
    <div class="container mx-auto px-4 py-8">
        <div class="text-center">
            <h1 class="text-4xl font-bold text-gray-900 mb-4">
                {{ t("app.welcome") }}
            </h1>
            <p class="text-lg text-gray-600">
                {{ t("app.tagline") }}
            </p>
        </div>
    </div>
</template>
