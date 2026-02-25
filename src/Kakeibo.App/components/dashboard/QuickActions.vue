<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import type { Wallet } from "@/stores/wallets";

const props = defineProps<{
    wallets: Wallet[];
}>();

const { t } = useI18n();
const router = useRouter();

const firstActiveWallet = computed(() => props.wallets[0] ?? null);
const hasWallets = computed(() => props.wallets.length > 0);

function recordTransaction() {
    if (!firstActiveWallet.value) return;
    router.push({ name: "transaction-record", params: { walletId: firstActiveWallet.value.id } });
}

function createWallet() {
    router.push({ name: "wallets-create" });
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("dashboard.quickActions.title") }}</CardTitle>
        </CardHeader>
        <CardContent class="flex flex-col gap-2">
            <Button :disabled="!hasWallets" @click="recordTransaction">
                {{ t("dashboard.quickActions.recordTransaction") }}
            </Button>
            <Button variant="outline" @click="createWallet">
                {{ t("dashboard.quickActions.createWallet") }}
            </Button>
        </CardContent>
    </Card>
</template>
