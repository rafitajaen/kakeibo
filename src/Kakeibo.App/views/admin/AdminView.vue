<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "@/stores/auth";
import { useRouter } from "vue-router";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import PlatformPolicyForm from "@/components/admin/PlatformPolicyForm.vue";
import UserManagement from "@/components/admin/UserManagement.vue";

const { t } = useI18n();
const auth = useAuthStore();
const router = useRouter();

const isAdmin = computed(() => auth.user?.role === "Admin");

// Redirect non-admins immediately
if (!isAdmin.value) {
    router.replace({ name: "home" });
}
</script>

<template>
    <div v-if="isAdmin" class="container mx-auto max-w-5xl py-8 px-4">
        <div class="mb-6">
            <h1 class="text-2xl font-bold">{{ t("admin.title") }}</h1>
            <p class="text-muted-foreground mt-1">{{ t("admin.subtitle") }}</p>
        </div>

        <Tabs default-value="users">
            <TabsList class="mb-6">
                <TabsTrigger value="users">{{ t("admin.tabs.users") }}</TabsTrigger>
                <TabsTrigger value="platform">{{ t("admin.tabs.platform") }}</TabsTrigger>
            </TabsList>

            <TabsContent value="users">
                <UserManagement />
            </TabsContent>

            <TabsContent value="platform">
                <PlatformPolicyForm />
            </TabsContent>
        </Tabs>
    </div>
</template>
