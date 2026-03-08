<script setup lang="ts">
import { computed, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { useCurrencyFormat } from "@/composables/useCurrencyFormat";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import WalletList from "@/components/wallets/WalletList.vue";

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();
const { formatCurrency } = useCurrencyFormat();

onMounted(async () => {
    await walletsStore.fetchWallets(true);
});

const activeWallets = computed(() => walletsStore.wallets.filter((w) => !w.isArchived));
const archivedWallets = computed(() => walletsStore.archivedWallets);

const assets = computed(() =>
    activeWallets.value.filter((w) => w.balance > 0).reduce((s, w) => s + w.balance, 0),
);
const debts = computed(() =>
    activeWallets.value.filter((w) => w.balance < 0).reduce((s, w) => s + Math.abs(w.balance), 0),
);
const net = computed(() => assets.value - debts.value);
</script>

<template>
    <div class="flex flex-col">
        <div class="flex items-center justify-between px-4 pt-4 pb-2">
            <h1 class="text-xl font-semibold">{{ t("wallets.title") }}</h1>
            <Button size="sm" @click="router.push({ name: 'wallets-create' })">
                {{ t("wallets.createNew") }}
            </Button>
        </div>

        <div v-if="walletsStore.isLoading" class="space-y-3 px-4 py-4">
            <Skeleton v-for="i in 3" :key="i" class="h-14 w-full rounded-lg" />
        </div>

        <Tabs v-else default-value="accounts">
            <TabsList class="mx-4 mb-2">
                <TabsTrigger value="accounts">{{ t("wallets.accounts") }}</TabsTrigger>
                <TabsTrigger value="finances">{{ t("wallets.myFinances") }}</TabsTrigger>
            </TabsList>

            <!-- Tab 1: All active wallets as list rows -->
            <TabsContent value="accounts" class="mt-0">
                <WalletList :wallets="activeWallets" :empty-message="t('wallets.emptyPersonal')" />

                <!-- Archived link -->
                <div v-if="archivedWallets.length > 0" class="px-4 py-3">
                    <button
                        class="text-sm text-muted-foreground underline underline-offset-2"
                        @click="router.push({ name: 'wallets' })"
                    >
                        {{ t("wallets.viewArchived") }} ({{ archivedWallets.length }})
                    </button>
                </div>
            </TabsContent>

            <!-- Tab 2: Assets vs Debts summary -->
            <TabsContent value="finances" class="mt-0 px-4 pt-4">
                <div class="grid grid-cols-2 gap-4 mb-4">
                    <div class="rounded-xl bg-muted/50 p-4">
                        <p class="text-xs text-muted-foreground mb-1">{{ t("wallets.assets") }}</p>
                        <p class="text-xl font-semibold text-income tabular-nums">
                            {{ formatCurrency(assets) }}
                        </p>
                    </div>
                    <div class="rounded-xl bg-muted/50 p-4">
                        <p class="text-xs text-muted-foreground mb-1">{{ t("wallets.debts") }}</p>
                        <p class="text-xl font-semibold text-expense tabular-nums">
                            {{ formatCurrency(debts) }}
                        </p>
                    </div>
                </div>
                <div class="rounded-xl border p-4">
                    <p class="text-sm text-muted-foreground mb-1">{{ t("wallets.net") }}</p>
                    <p
                        class="text-2xl font-bold tabular-nums"
                        :class="net >= 0 ? 'text-income' : 'text-expense'"
                    >
                        {{ formatCurrency(net) }}
                    </p>
                </div>
            </TabsContent>
        </Tabs>
    </div>
</template>
