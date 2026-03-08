<script setup lang="ts">
import type { Wallet } from "@/stores/wallets";
import WalletCard from "@/components/wallets/WalletCard.vue";

defineProps<{
    wallets: Wallet[];
    emptyMessage: string;
}>();
</script>

<template>
    <div
        v-if="wallets.length === 0"
        class="flex flex-col items-center justify-center py-12 text-center"
    >
        <p class="text-muted-foreground">{{ emptyMessage }}</p>
    </div>
    <div v-else class="divide-y divide-border">
        <WalletCard v-for="wallet in wallets" :key="wallet.id" :wallet="wallet" />
    </div>
</template>
