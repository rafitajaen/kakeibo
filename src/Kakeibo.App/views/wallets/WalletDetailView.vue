<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();

const apiError = ref<string | null>(null);

onMounted(async () => {
    try {
        await walletsStore.fetchWallet(route.params.id as string);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
            apiError.value = t("wallets.errors.notFound");
        } else if (status === 403) {
            apiError.value = t("wallets.errors.forbidden");
        } else {
            apiError.value = t("wallets.errors.unexpected");
        }
    }
});

async function handleArchive() {
    if (!confirm(t("wallets.actions.archiveConfirm"))) return;
    try {
        await walletsStore.archiveWallet(route.params.id as string);
        await router.push({ name: "wallets" });
    } catch {
        apiError.value = t("wallets.errors.unexpected");
    }
}
</script>

<template>
    <div class="container mx-auto max-w-2xl px-4 py-8">
        <p v-if="apiError" class="text-destructive">{{ apiError }}</p>

        <template v-else-if="walletsStore.currentWallet">
            <Card>
                <CardHeader class="flex flex-row items-start justify-between gap-2">
                    <CardTitle>{{ walletsStore.currentWallet.name }}</CardTitle>
                    <Badge variant="secondary">
                        {{ t(`wallets.type.${walletsStore.currentWallet.type.toLowerCase()}`) }}
                    </Badge>
                </CardHeader>

                <CardContent class="space-y-2">
                    <div class="flex items-baseline gap-2">
                        <span class="text-3xl font-semibold tabular-nums">
                            {{ walletsStore.currentWallet.balance.toFixed(2) }}
                        </span>
                        <span class="text-muted-foreground">{{
                            walletsStore.currentWallet.currency
                        }}</span>
                    </div>
                </CardContent>

                <CardFooter class="flex flex-wrap gap-2">
                    <Button
                        @click="
                            router.push({
                                name: 'transactions',
                                params: { walletId: walletsStore.currentWallet.id },
                            })
                        "
                    >
                        {{ t("transactions.viewAll") }}
                    </Button>
                    <Button
                        variant="outline"
                        @click="
                            router.push({
                                name: 'transaction-record',
                                params: { walletId: walletsStore.currentWallet.id },
                            })
                        "
                    >
                        {{ t("transactions.createNew") }}
                    </Button>
                    <Button
                        variant="outline"
                        @click="
                            router.push({
                                name: 'wallet-edit',
                                params: { id: walletsStore.currentWallet.id },
                            })
                        "
                    >
                        {{ t("wallets.actions.edit") }}
                    </Button>
                    <Button
                        v-if="walletsStore.currentWallet.type === 'Shared'"
                        variant="outline"
                        @click="
                            router.push({
                                name: 'wallet-members',
                                params: { id: walletsStore.currentWallet.id },
                            })
                        "
                    >
                        {{ t("wallets.members.title") }}
                    </Button>
                    <Button
                        variant="ghost"
                        class="text-destructive hover:text-destructive"
                        @click="handleArchive"
                    >
                        {{ t("wallets.actions.archive") }}
                    </Button>
                </CardFooter>
            </Card>
        </template>

        <p v-else class="text-muted-foreground">{{ t("common.loading") }}</p>
    </div>
</template>
