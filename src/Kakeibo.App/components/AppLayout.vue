<script setup lang="ts">
import { onMounted } from "vue";
import { SidebarProvider, SidebarInset } from "@/components/ui/sidebar";
import { usePushNotifications } from "@/composables/usePushNotifications";
import AppSidebar from "@/components/AppSidebar.vue";
import SiteHeader from "@/components/SiteHeader.vue";

const { initPushSubscription } = usePushNotifications();

onMounted(() => {
    initPushSubscription();
});
</script>

<template>
    <SidebarProvider
        :style="{
            '--sidebar-width': 'calc(var(--spacing) * 64)',
            '--header-height': 'calc(var(--spacing) * 12 + 1px)',
        }"
    >
        <AppSidebar />
        <SidebarInset>
            <SiteHeader />
            <div class="@container/main flex flex-1 flex-col gap-2">
                <RouterView />
            </div>
        </SidebarInset>
    </SidebarProvider>
</template>
