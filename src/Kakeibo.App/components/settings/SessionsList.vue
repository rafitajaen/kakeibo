<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { Button } from "@/components/ui/button";
import { useSettingsStore } from "@/stores/settings";

const { t } = useI18n();
const settingsStore = useSettingsStore();

const revokingId = ref<string | null>(null);

async function revokeSession(sessionId: string) {
    revokingId.value = sessionId;
    try {
        await settingsStore.revokeSession(sessionId);
    } finally {
        revokingId.value = null;
    }
}
</script>

<template>
    <div class="space-y-4">
        <p class="text-sm text-muted-foreground">{{ t("settings.sessions.description") }}</p>
        <div
            v-if="settingsStore.sessions.length === 0"
            class="text-sm text-muted-foreground py-4 text-center"
        >
            {{ t("settings.sessions.empty") }}
        </div>
        <div v-else class="space-y-3">
            <div
                v-for="session in settingsStore.sessions"
                :key="session.id"
                class="flex items-start justify-between rounded-lg border p-3 gap-3"
            >
                <div class="space-y-1 min-w-0">
                    <p class="text-sm font-medium truncate">
                        {{ session.userAgent ?? t("settings.sessions.unknownDevice") }}
                    </p>
                    <p class="text-xs text-muted-foreground">
                        {{ session.ipAddress ?? "—" }} · {{ session.createdAt }}
                    </p>
                </div>
                <Button
                    variant="outline"
                    size="sm"
                    :disabled="revokingId === session.id"
                    @click="revokeSession(session.id)"
                >
                    {{ t("settings.sessions.revoke") }}
                </Button>
            </div>
        </div>
    </div>
</template>
