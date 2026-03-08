<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import WalletList from "@/components/wallets/WalletList.vue";

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();

onMounted(async () => {
    await walletsStore.fetchWallets(true);
});
</script>

<template>
    <div class="container mx-auto max-w-5xl px-4 py-8">
        <div class="mb-6 flex items-center justify-between">
            <h1 class="text-2xl font-semibold">{{ t("wallets.title") }}</h1>
            <Button @click="router.push({ name: 'wallets-create' })">
                {{ t("wallets.createNew") }}
            </Button>
        </div>

        <div v-if="walletsStore.isLoading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Skeleton v-for="i in 3" :key="i" class="h-32 w-full rounded-lg" />
        </div>

        <Tabs v-else default-value="personal">
            <TabsList class="mb-4">
                <TabsTrigger value="personal">{{ t("wallets.personal") }}</TabsTrigger>
                <TabsTrigger value="shared">{{ t("wallets.shared") }}</TabsTrigger>
                <TabsTrigger value="archived">{{ t("wallets.archived") }}</TabsTrigger>
            </TabsList>

            <TabsContent value="personal">
                <WalletList
                    :wallets="walletsStore.personalWallets"
                    :empty-message="t('wallets.emptyPersonal')"
                />
            </TabsContent>

            <TabsContent value="shared">
                <WalletList
                    :wallets="walletsStore.sharedWallets"
                    :empty-message="t('wallets.emptyShared')"
                />
            </TabsContent>

            <TabsContent value="archived">
                <WalletList
                    :wallets="walletsStore.archivedWallets"
                    :empty-message="t('wallets.emptyArchived')"
                />
            </TabsContent>
        </Tabs>
    </div>
</template>
