<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import ProfileForm from "@/components/settings/ProfileForm.vue";
import PasswordChangeForm from "@/components/settings/PasswordChangeForm.vue";
import SessionsList from "@/components/settings/SessionsList.vue";
import DeleteAccountSection from "@/components/settings/DeleteAccountSection.vue";
import { useSettingsStore } from "@/stores/settings";

const { t } = useI18n();
const settingsStore = useSettingsStore();

onMounted(() => {
    settingsStore.fetchSessions();
});
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-2xl">
        <h1 class="text-2xl font-bold mb-6">{{ t("settings.title") }}</h1>
        <Tabs default-value="profile">
            <TabsList class="mb-6">
                <TabsTrigger value="profile">{{ t("settings.profile.title") }}</TabsTrigger>
                <TabsTrigger value="security">{{ t("settings.security.title") }}</TabsTrigger>
                <TabsTrigger value="sessions">{{ t("settings.sessions.title") }}</TabsTrigger>
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
        </Tabs>
    </div>
</template>
