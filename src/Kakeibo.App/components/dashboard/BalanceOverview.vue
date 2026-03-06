<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { Wallet } from "@/stores/wallets";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";

const props = defineProps<{
    wallets: Wallet[];
    isLoading: boolean;
}>();

const { t } = useI18n();
const { formatCurrency } = useCurrencyFormat();

const totalBalance = computed(() => props.wallets.reduce((sum, w) => sum + w.balance, 0));

function formatAmount(amount: number): string {
    return formatCurrency.value(amount);
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle>{{ t("dashboard.balanceOverview.title") }}</CardTitle>
        </CardHeader>
        <CardContent>
            <p v-if="isLoading" class="text-muted-foreground">{{ t("common.loading") }}</p>
            <div v-else-if="wallets.length === 0" class="text-sm text-muted-foreground">
                {{ t("dashboard.balanceOverview.noWallets") }}
            </div>
            <div v-else>
                <p class="text-3xl font-bold mb-4">
                    {{ formatAmount(totalBalance) }}
                </p>
                <div class="space-y-2">
                    <div
                        v-for="wallet in wallets"
                        :key="wallet.id"
                        class="flex justify-between items-center text-sm"
                    >
                        <span class="text-muted-foreground">{{ wallet.name }}</span>
                        <span class="font-medium">{{ formatAmount(wallet.balance) }}</span>
                    </div>
                </div>
            </div>
        </CardContent>
    </Card>
</template>
