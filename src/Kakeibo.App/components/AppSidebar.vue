<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import {
    Activity,
    LayoutDashboard,
    PiggyBank,
    Plus,
    Repeat,
    Tag,
    Target,
    Users,
    Wallet,
} from "lucide-vue-next";
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import NavMain from "@/components/nav/NavMain.vue";
import NavUser from "@/components/nav/NavUser.vue";
import WalletPicker from "@/components/wallets/WalletPicker.vue";
import { useWalletsStore } from "@/stores/wallets";

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();

const showWalletPicker = ref(false);
const selectedWallet = ref<string | null>(null);

const navMainItems = [
    {
        title: computed(() => t("app.home")),
        routeName: "home",
        icon: LayoutDashboard,
    },
    {
        title: computed(() => t("wallets.title")),
        routeName: "wallets",
        icon: Wallet,
    },
    {
        title: computed(() => t("categories.title")),
        routeName: "categories",
        icon: Tag,
    },
    {
        title: computed(() => t("app.budgets")),
        routeName: "budgets",
        icon: PiggyBank,
    },
    {
        title: computed(() => t("app.goals")),
        routeName: "goals",
        icon: Target,
    },
    {
        title: computed(() => t("app.recurring")),
        routeName: "recurring",
        icon: Repeat,
    },
    {
        title: computed(() => t("friends.title")),
        routeName: "friends",
        icon: Users,
    },
    {
        title: computed(() => t("activity.title")),
        routeName: "activity",
        icon: Activity,
    },
];

function handleNewTransaction() {
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
    <Sidebar collapsible="icon">
        <SidebarHeader>
            <SidebarMenu>
                <SidebarMenuItem>
                    <SidebarMenuButton as-child size="lg">
                        <router-link :to="{ name: 'home' }">
                            <span class="font-bold text-lg leading-tight">K</span>
                            <span class="ml-1 font-semibold">Kakeibo</span>
                        </router-link>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>

            <!-- New Transaction button — visible when sidebar is expanded -->
            <SidebarMenu>
                <SidebarMenuItem>
                    <SidebarMenuButton
                        class="bg-primary text-primary-foreground hover:bg-primary/90 hover:text-primary-foreground"
                        @click="handleNewTransaction"
                    >
                        <Plus class="size-4" />
                        <span>{{ t("transactions.createNew") }}</span>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarHeader>

        <SidebarContent>
            <NavMain :items="navMainItems.map((i) => ({ ...i, title: i.title.value }))" />
        </SidebarContent>

        <SidebarFooter>
            <NavUser />
        </SidebarFooter>
    </Sidebar>

    <!-- Wallet picker dialog for new transaction when multiple wallets exist -->
    <Dialog v-model:open="showWalletPicker">
        <DialogContent class="sm:max-w-sm">
            <DialogHeader>
                <DialogTitle>{{ t("transactions.selectWallet") }}</DialogTitle>
            </DialogHeader>
            <WalletPicker v-model="selectedWallet" />
            <Button :disabled="!selectedWallet" class="mt-2" @click="confirmWalletPick">
                {{ t("common.next") }}
            </Button>
        </DialogContent>
    </Dialog>
</template>
