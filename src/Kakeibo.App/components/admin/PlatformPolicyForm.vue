<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useAdminStore } from "@/stores/admin";
import { Switch } from "@/components/ui/switch";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

const { t } = useI18n();
const admin = useAdminStore();

const registrationEnabled = ref(true);
const maintenanceMode = ref(false);
const saving = ref(false);
const saved = ref(false);

onMounted(async () => {
    await admin.fetchPolicy();
    if (admin.policy) {
        registrationEnabled.value = admin.policy.registrationEnabled;
        maintenanceMode.value = admin.policy.maintenanceMode;
    }
});

async function save() {
    saving.value = true;
    saved.value = false;
    try {
        await admin.updatePolicy({
            registrationEnabled: registrationEnabled.value,
            maintenanceMode: maintenanceMode.value,
        });
        saved.value = true;
        setTimeout(() => (saved.value = false), 2000);
    } finally {
        saving.value = false;
    }
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("admin.platform.title") }}</CardTitle>
            <CardDescription>{{ t("admin.platform.description") }}</CardDescription>
        </CardHeader>
        <CardContent>
            <Skeleton v-if="admin.loadingPolicy" class="h-24 w-full" />
            <div v-else class="space-y-6">
                <!-- Registration toggle -->
                <div class="flex items-center justify-between">
                    <div>
                        <Label>{{ t("admin.platform.registration.label") }}</Label>
                        <p class="text-sm text-muted-foreground">
                            {{ t("admin.platform.registration.description") }}
                        </p>
                    </div>
                    <Switch v-model:checked="registrationEnabled" />
                </div>

                <!-- Maintenance mode toggle -->
                <div class="flex items-center justify-between">
                    <div>
                        <Label>{{ t("admin.platform.maintenance.label") }}</Label>
                        <p class="text-sm text-muted-foreground">
                            {{ t("admin.platform.maintenance.description") }}
                        </p>
                    </div>
                    <Switch v-model:checked="maintenanceMode" />
                </div>

                <div class="flex items-center gap-3">
                    <Button @click="save" :disabled="saving">
                        {{ saving ? t("common.saving") : t("common.save") }}
                    </Button>
                    <span v-if="saved" class="text-sm text-green-600">{{ t("common.saved") }}</span>
                </div>
            </div>
        </CardContent>
    </Card>
</template>
