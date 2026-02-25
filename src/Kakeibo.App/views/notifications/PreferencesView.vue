<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import PreferencesForm from "@/components/notifications/PreferencesForm.vue";
import { useNotificationsStore } from "@/stores/notifications";

const { t } = useI18n();
const store = useNotificationsStore();

onMounted(async () => {
    await store.fetchPreferences();
});
</script>

<template>
    <div class="container max-w-2xl py-6">
        <h1 class="text-2xl font-bold mb-6">{{ t("notifications.prefs.title") }}</h1>
        <PreferencesForm />
    </div>
</template>
