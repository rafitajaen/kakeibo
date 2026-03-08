<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
import { Wallet } from "lucide-vue-next";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import type { Wallet as WalletType } from "@/stores/wallets";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

const props = defineProps<{
    wallet: WalletType;
}>();

const { t } = useI18n();
const router = useRouter();
const { formatCurrency } = useCurrencyFormat();

function getIcon(name: string | null) {
    if (!name) return Wallet;
    return (LucideIcons as Record<string, unknown>)[name] ?? Wallet;
}
</script>

<template>
    <button
        class="flex w-full items-center gap-3 px-4 py-3 hover:bg-muted/50 active:bg-muted/70 text-left"
        @click="router.push({ name: 'wallet-detail', params: { id: wallet.id } })"
    >
        <!-- Icon square -->
        <div
            class="flex size-10 shrink-0 items-center justify-center rounded-xl"
            :style="{ backgroundColor: wallet.backgroundColor ?? 'oklch(0.488 0.243 264.376)' }"
        >
            <component :is="getIcon(wallet.icon)" class="size-5 text-white" />
        </div>

        <!-- Name + type -->
        <div class="flex min-w-0 flex-1 flex-col">
            <span class="truncate text-base font-medium">{{ wallet.name }}</span>
            <span class="text-xs text-muted-foreground">
                {{ t(`wallets.type.${wallet.type.toLowerCase()}`) }}
            </span>
        </div>

        <!-- Balance -->
        <span
            class="shrink-0 text-base font-semibold tabular-nums"
            :class="wallet.balance >= 0 ? 'text-income' : 'text-expense'"
        >
            {{ formatCurrency(wallet.balance) }}
        </span>
    </button>
</template>
