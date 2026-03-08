<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { LayoutDashboard, Wallet, ReceiptText, MoreHorizontal } from "lucide-vue-next";

const emit = defineEmits<{
    (e: "open-more"): void;
    (e: "open-wallet-picker"): void;
}>();

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const tabs = [
    {
        key: "home",
        icon: LayoutDashboard,
        label: () => t("nav.tabs.home"),
        routes: ["home"],
        action: () => router.push({ name: "home" }),
    },
    {
        key: "wallets",
        icon: Wallet,
        label: () => t("nav.tabs.wallets"),
        routes: [
            "wallets",
            "wallet-detail",
            "wallet-members",
            "wallet-edit",
            "wallet-new",
            "wallet-invite",
        ],
        action: () => router.push({ name: "wallets" }),
    },
    {
        key: "transactions",
        icon: ReceiptText,
        label: () => t("nav.tabs.transactions"),
        routes: ["transactions-global", "transactions", "transaction-record", "transaction-edit"],
        action: () => router.push({ name: "transactions-global" }),
    },
    {
        key: "more",
        icon: MoreHorizontal,
        label: () => t("nav.tabs.more"),
        routes: [
            "budgets",
            "goals",
            "recurring",
            "friends",
            "friend-requests",
            "activity",
            "settings",
            "notification-preferences",
        ],
        action: () => emit("open-more"),
    },
] as const;

const activeTab = computed(() => {
    const name = route.name as string;
    return tabs.find((tab) => (tab.routes as readonly string[]).includes(name))?.key ?? "";
});
</script>

<template>
    <nav
        class="fixed bottom-0 inset-x-0 z-40 h-16 bg-background border-t flex items-center md:hidden pb-safe"
    >
        <button
            v-for="tab in tabs"
            :key="tab.key"
            class="flex flex-1 flex-col items-center justify-center gap-0.5 py-1 relative"
            :class="activeTab === tab.key ? 'text-primary' : 'text-muted-foreground'"
            @click="tab.action"
        >
            <!-- Active indicator -->
            <span
                v-if="activeTab === tab.key"
                class="absolute top-0 left-1/2 -translate-x-1/2 h-0.5 w-8 rounded-full bg-primary"
            />
            <component :is="tab.icon" class="size-5" />
            <span class="text-[10px] leading-none">{{ tab.label() }}</span>
        </button>
    </nav>
</template>
