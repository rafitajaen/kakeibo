<script setup lang="ts">
import { useI18n } from "vue-i18n";
import type { Wallet } from "@/stores/wallets";
import WalletCard from "@/components/wallets/WalletCard.vue";

defineProps<{
    wallets: Wallet[];
    emptyMessage: string;
}>();

const { t } = useI18n();
</script>

<template>
    <div
        v-if="wallets.length === 0"
        class="flex flex-col items-center justify-center py-12 text-center"
    >
        <p class="text-muted-foreground">{{ emptyMessage }}</p>
    </div>
    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <WalletCard v-for="wallet in wallets" :key="wallet.id" :wallet="wallet" />
    </div>
</template>
