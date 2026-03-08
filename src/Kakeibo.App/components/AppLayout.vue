<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { Plus, Bell } from "lucide-vue-next";
import { SidebarProvider, SidebarInset } from "@/components/ui/sidebar";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { usePushNotifications } from "@/composables/usePushNotifications";
import { useScreenSize } from "@/composables/useScreenSize";
import { useWalletsStore } from "@/stores/wallets";
import AppSidebar from "@/components/AppSidebar.vue";
import SiteHeader from "@/components/SiteHeader.vue";
import MobileHeader from "@/components/MobileHeader.vue";
import MobileNavDrawer from "@/components/MobileNavDrawer.vue";
import BottomTabBar from "@/components/BottomTabBar.vue";
import MoreSheet from "@/components/MoreSheet.vue";
import WalletPicker from "@/components/wallets/WalletPicker.vue";

const { initPushSubscription } = usePushNotifications();
const { isMobile } = useScreenSize();
const walletsStore = useWalletsStore();
const route = useRoute();
const router = useRouter();

const showDrawer = ref(false);
const showMoreSheet = ref(false);
const showWalletPicker = ref(false);
const selectedWallet = ref<string | null>(null);

onMounted(() => {
    initPushSubscription();
});

const isTransactionsTab = computed(() =>
    ["transactions-global", "transactions", "transaction-record", "transaction-edit"].includes(
        route.name as string,
    ),
);

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
    <!-- MOBILE layout -->
    <template v-if="isMobile">
        <MobileHeader @open-drawer="showDrawer = true">
            <template #action>
                <button v-if="isTransactionsTab" @click="handleFab">
                    <Plus class="size-5" />
                </button>
                <router-link v-else :to="{ name: 'notification-preferences' }">
                    <Bell class="size-5" />
                </router-link>
            </template>
        </MobileHeader>

        <MobileNavDrawer v-model:open="showDrawer" />

        <main class="pt-14 pb-16 min-h-screen @container/main">
            <RouterView />
        </main>

        <!-- FAB for Transactions tab — sits above the tab bar -->
        <button
            v-if="isTransactionsTab"
            class="fixed bottom-20 right-4 z-50 flex size-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg active:scale-95 transition-transform"
            @click="handleFab"
        >
            <Plus class="size-6" />
        </button>

        <BottomTabBar
            @open-more="showMoreSheet = true"
            @open-wallet-picker="showWalletPicker = true"
        />

        <MoreSheet v-model:open="showMoreSheet" />
    </template>

    <!-- DESKTOP layout -->
    <template v-else>
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

        <!-- Desktop FAB -->
        <button
            class="fixed bottom-6 right-6 z-50 hidden sm:flex size-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg transition-transform active:scale-95"
            @click="handleFab"
        >
            <Plus class="size-6" />
        </button>
    </template>

    <!-- Wallet picker dialog — shared between mobile and desktop -->
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
