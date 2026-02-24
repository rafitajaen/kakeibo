<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useWalletsStore, type Wallet } from "@/stores/wallets";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const props = defineProps<{
    wallet: Wallet;
}>();

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();

async function handleArchive() {
    if (!confirm(t("wallets.actions.archiveConfirm"))) return;
    await walletsStore.archiveWallet(props.wallet.id);
}
</script>

<template>
    <Card class="flex flex-col">
        <CardHeader class="flex flex-row items-start justify-between gap-2 pb-2">
            <CardTitle class="text-base leading-tight">{{ wallet.name }}</CardTitle>
            <Badge variant="secondary" class="shrink-0">
                {{ t(`wallets.type.${wallet.type.toLowerCase()}`) }}
            </Badge>
        </CardHeader>

        <CardContent class="flex-1">
            <p class="text-2xl font-semibold tabular-nums">
                {{ wallet.balance.toFixed(2) }}
                <span class="text-sm font-normal text-muted-foreground ml-1">{{
                    wallet.currency
                }}</span>
            </p>
        </CardContent>

        <CardFooter class="flex gap-2 pt-2">
            <Button
                variant="outline"
                size="sm"
                @click="router.push({ name: 'wallet-edit', params: { id: wallet.id } })"
            >
                {{ t("wallets.actions.edit") }}
            </Button>
            <Button
                v-if="wallet.type === 'Shared'"
                variant="outline"
                size="sm"
                @click="router.push({ name: 'wallet-members', params: { id: wallet.id } })"
            >
                {{ t("wallets.members.title") }}
            </Button>
            <Button
                variant="ghost"
                size="sm"
                class="text-destructive hover:text-destructive"
                @click="handleArchive"
            >
                {{ t("wallets.actions.archive") }}
            </Button>
        </CardFooter>
    </Card>
</template>
