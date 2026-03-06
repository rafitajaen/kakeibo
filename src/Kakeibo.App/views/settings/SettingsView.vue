<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import api from "@/lib/axios";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import ProfileForm from "@/components/settings/ProfileForm.vue";
import PasswordChangeForm from "@/components/settings/PasswordChangeForm.vue";
import SessionsList from "@/components/settings/SessionsList.vue";
import DeleteAccountSection from "@/components/settings/DeleteAccountSection.vue";
import ImportExportSection from "@/components/settings/ImportExportSection.vue";
import { useSettingsStore } from "@/stores/settings";
import { useWalletsStore } from "@/stores/wallets";

const { t } = useI18n();
const settingsStore = useSettingsStore();
const walletsStore = useWalletsStore();

const hasSeedData = ref(false);
const showDeleteSeedConfirm = ref(false);
const isSeedLoading = ref(false);
const seedMessage = ref<string | null>(null);

onMounted(async () => {
    settingsStore.fetchSessions();
    await walletsStore.fetchWallets(false);
    // Check if any active wallet is seed data — we detect it indirectly via the API response
    // The frontend doesn't have the isSeedData flag in the wallet interface, so we just
    // call GET /api/users/me/seed-data to probe. For simplicity, we make the button always visible.
    hasSeedData.value = true;
});

async function activateSeedData() {
    isSeedLoading.value = true;
    seedMessage.value = null;
    try {
        const res = await api.post("/api/users/me/seed-data");
        seedMessage.value =
            res.status === 201
                ? t("settings.data.seedCreated")
                : t("settings.data.seedAlreadyExists");
        await walletsStore.fetchWallets(false);
    } finally {
        isSeedLoading.value = false;
    }
}

async function deleteSeedData() {
    showDeleteSeedConfirm.value = false;
    isSeedLoading.value = true;
    seedMessage.value = null;
    try {
        await api.delete("/api/users/me/seed-data");
        seedMessage.value = t("settings.data.seedDeleted");
        await walletsStore.fetchWallets(false);
    } finally {
        isSeedLoading.value = false;
    }
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-2xl">
        <h1 class="text-2xl font-bold mb-6">{{ t("settings.title") }}</h1>
        <Tabs default-value="profile">
            <TabsList class="mb-6">
                <TabsTrigger value="profile">{{ t("settings.profile.title") }}</TabsTrigger>
                <TabsTrigger value="security">{{ t("settings.security.title") }}</TabsTrigger>
                <TabsTrigger value="sessions">{{ t("settings.sessions.title") }}</TabsTrigger>
                <TabsTrigger value="data">{{ t("settings.data.title") }}</TabsTrigger>
                <TabsTrigger value="import-export">{{
                    t("settings.importExport.tabTitle")
                }}</TabsTrigger>
            </TabsList>

            <TabsContent value="profile">
                <Card>
                    <CardHeader>
                        <CardTitle>{{ t("settings.profile.title") }}</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <ProfileForm />
                    </CardContent>
                </Card>
            </TabsContent>

            <TabsContent value="security">
                <div class="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>{{ t("settings.security.changePassword") }}</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <PasswordChangeForm />
                        </CardContent>
                    </Card>
                    <DeleteAccountSection />
                </div>
            </TabsContent>

            <TabsContent value="sessions">
                <Card>
                    <CardHeader>
                        <CardTitle>{{ t("settings.sessions.title") }}</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <SessionsList />
                    </CardContent>
                </Card>
            </TabsContent>

            <TabsContent value="data">
                <Card>
                    <CardHeader>
                        <CardTitle>{{ t("settings.data.title") }}</CardTitle>
                    </CardHeader>
                    <CardContent class="space-y-4">
                        <p class="text-sm text-muted-foreground">
                            {{ t("settings.data.description") }}
                        </p>

                        <p v-if="seedMessage" class="text-sm text-green-600 dark:text-green-400">
                            {{ seedMessage }}
                        </p>

                        <div class="flex flex-col gap-2 sm:flex-row">
                            <Button :disabled="isSeedLoading" @click="activateSeedData">
                                {{ t("settings.data.activate") }}
                            </Button>
                            <Button
                                variant="outline"
                                class="text-destructive hover:text-destructive"
                                :disabled="isSeedLoading"
                                @click="showDeleteSeedConfirm = true"
                            >
                                {{ t("settings.data.delete") }}
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            </TabsContent>

            <TabsContent value="import-export">
                <Card>
                    <CardHeader>
                        <CardTitle>{{ t("settings.importExport.tabTitle") }}</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <ImportExportSection />
                    </CardContent>
                </Card>
            </TabsContent>
        </Tabs>
    </div>

    <AlertDialog v-model:open="showDeleteSeedConfirm">
        <AlertDialogContent>
            <AlertDialogHeader>
                <AlertDialogTitle>{{ t("settings.data.deleteTitle") }}</AlertDialogTitle>
                <AlertDialogDescription>{{
                    t("settings.data.deleteConfirm")
                }}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
                <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                <AlertDialogAction
                    class="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    @click="deleteSeedData"
                >
                    {{ t("settings.data.delete") }}
                </AlertDialogAction>
            </AlertDialogFooter>
        </AlertDialogContent>
    </AlertDialog>
</template>
