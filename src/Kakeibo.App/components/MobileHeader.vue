<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useUserNav } from "@/composables/useUserNav";
import { useWalletsStore } from "@/stores/wallets";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

defineEmits<{ (e: "open-drawer"): void }>();

const { t } = useI18n();
const { initials, auth } = useUserNav();
const walletsStore = useWalletsStore();
const { formatCurrency } = useCurrencyFormat();

const netBalance = computed(() =>
    walletsStore.activeWallets.reduce((sum, w) => sum + w.balance, 0),
);
</script>

<template>
    <header
        class="fixed top-0 inset-x-0 z-40 h-14 bg-background border-b flex items-center px-4 gap-3 md:hidden"
    >
        <button class="shrink-0" @click="$emit('open-drawer')">
            <Avatar class="size-8 rounded-lg">
                <AvatarImage v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" />
                <AvatarFallback class="rounded-lg text-xs">{{ initials }}</AvatarFallback>
            </Avatar>
        </button>

        <div class="flex-1 text-center">
            <p class="text-xs text-muted-foreground leading-none">{{ t("nav.netBalance") }}</p>
            <p
                class="text-base font-semibold tabular-nums leading-tight"
                :class="netBalance >= 0 ? 'text-income' : 'text-expense'"
            >
                {{ formatCurrency(netBalance) }}
            </p>
        </div>

        <div class="shrink-0">
            <slot name="action" />
        </div>
    </header>
</template>
