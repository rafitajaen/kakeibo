<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { Plus } from "lucide-vue-next";
import { SidebarProvider, SidebarInset } from "@/components/ui/sidebar";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { usePushNotifications } from "@/composables/usePushNotifications";
import { useWalletsStore } from "@/stores/wallets";
import AppSidebar from "@/components/AppSidebar.vue";
import SiteHeader from "@/components/SiteHeader.vue";
import WalletPicker from "@/components/wallets/WalletPicker.vue";

const { initPushSubscription } = usePushNotifications();
const walletsStore = useWalletsStore();
const router = useRouter();

const showWalletPicker = ref(false);
const selectedWallet = ref<string | null>(null);

onMounted(() => {
    initPushSubscription();
});

function handleFab() {
    const active = walletsStore.activeWallets;
    if (active.length === 1) {
        router.push({ name: "transaction-record", params: { walletId: active[0].id } });
    } else {
        selectedWallet.value = null;
        showWalletPicker.value = true;
    }
}

function confirmWalletPick() {
    if (!selectedWallet.value) return;
    showWalletPicker.value = false;
    router.push({ name: "transaction-record", params: { walletId: selectedWallet.value } });
}
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

    <!-- Mobile FAB — only visible on small screens -->
    <button
        class="fixed bottom-6 right-6 z-50 flex size-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg transition-transform active:scale-95 sm:hidden"
        @click="handleFab"
    >
        <Plus class="size-6" />
    </button>

    <!-- Wallet picker dialog for FAB when multiple wallets exist -->
    <Dialog v-model:open="showWalletPicker">
        <DialogContent class="sm:max-w-sm">
            <DialogHeader>
                <DialogTitle>Select wallet</DialogTitle>
            </DialogHeader>
            <WalletPicker v-model="selectedWallet" />
            <Button :disabled="!selectedWallet" class="mt-2" @click="confirmWalletPick">
                Next
            </Button>
        </DialogContent>
    </Dialog>
</template>
