<script setup lang="ts">
import * as LucideIcons from "lucide-vue-next";
import { Wallet } from "lucide-vue-next";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useWalletsStore, type Wallet as WalletType } from "@/stores/wallets";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import EditWalletDialog from "@/components/wallets/EditWalletDialog.vue";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

const props = defineProps<{
    wallet: WalletType;
}>();

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();
const { formatCurrency } = useCurrencyFormat();

function getIcon(name: string | null) {
    if (!name) return Wallet;
    return (LucideIcons as Record<string, unknown>)[name] ?? Wallet;
}

async function handleUpdated() {
    await walletsStore.fetchWallets(true);
}
</script>

<template>
    <Card class="flex flex-col overflow-hidden">
        <!-- Colored header strip when a background color is set -->
        <div
            v-if="wallet.backgroundColor"
            class="flex items-center gap-2 px-4 py-3"
            :style="{ backgroundColor: wallet.backgroundColor, color: wallet.textColor ?? '#fff' }"
        >
            <component :is="getIcon(wallet.icon)" class="size-4 shrink-0" />
            <span class="font-medium truncate">{{ wallet.name }}</span>
            <Badge
                variant="secondary"
                class="ml-auto shrink-0 text-xs bg-white/20 text-inherit border-0"
            >
                {{ t(`wallets.type.${wallet.type.toLowerCase()}`) }}
            </Badge>
        </div>

        <CardHeader v-else class="flex flex-row items-start justify-between gap-2 pb-2">
            <div class="flex items-center gap-2 min-w-0">
                <component
                    :is="getIcon(wallet.icon)"
                    class="size-4 shrink-0 text-muted-foreground"
                />
                <CardTitle class="text-base leading-tight truncate">{{ wallet.name }}</CardTitle>
            </div>
            <Badge variant="secondary" class="shrink-0">
                {{ t(`wallets.type.${wallet.type.toLowerCase()}`) }}
            </Badge>
        </CardHeader>

        <CardContent class="flex-1 pt-3">
            <p class="text-2xl font-semibold tabular-nums">
                {{ formatCurrency(wallet.balance) }}
            </p>
        </CardContent>

        <CardFooter class="flex gap-2 pt-2">
            <EditWalletDialog :wallet="wallet" @updated="handleUpdated">
                <Button variant="outline" size="sm">
                    {{ t("wallets.actions.edit") }}
                </Button>
            </EditWalletDialog>
            <Button
                v-if="wallet.type === 'Shared'"
                variant="outline"
                size="sm"
                @click="router.push({ name: 'wallet-members', params: { id: wallet.id } })"
            >
                {{ t("wallets.members.title") }}
            </Button>
        </CardFooter>
    </Card>
</template>
