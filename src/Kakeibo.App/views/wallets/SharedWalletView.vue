<script setup lang="ts">
import { onMounted, ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { useAuthStore } from "@/stores/auth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import MemberList from "@/components/wallets/MemberList.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();
const authStore = useAuthStore();

const walletId = route.params.id as string;
const memberList = ref(walletsStore.members.get(walletId) ?? []);
const apiError = ref<string | null>(null);

const currentUserId = computed(() => authStore.user?.id ?? "");
const isOwner = computed(() =>
    memberList.value.some((m) => m.userId === currentUserId.value && m.isOwner),
);

onMounted(async () => {
    try {
        await walletsStore.fetchWallet(walletId);
        memberList.value = await walletsStore.fetchMembers(walletId);
    } catch {
        apiError.value = t("wallets.members.errors.unexpected");
    }
});

async function handleRemove(userId: string) {
    try {
        await walletsStore.removeMember(walletId, userId);
        memberList.value = walletsStore.members.get(walletId) ?? [];
        // If the current user left, redirect to wallets list.
        if (userId === currentUserId.value) {
            await router.push({ name: "wallets" });
        }
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 403) {
            apiError.value = t("wallets.members.errors.forbidden");
        } else if (status === 404) {
            apiError.value = t("wallets.members.errors.notFound");
        } else {
            apiError.value = t("wallets.members.errors.unexpected");
        }
    }
}
</script>

<template>
    <div class="container mx-auto max-w-2xl px-4 py-8">
        <div class="mb-6 flex items-center justify-between">
            <h1 class="text-2xl font-semibold">
                {{ walletsStore.currentWallet?.name ?? t("wallets.members.title") }}
            </h1>
            <Button
                v-if="isOwner"
                @click="router.push({ name: 'wallet-invite', params: { id: walletId } })"
            >
                {{ t("wallets.members.invite") }}
            </Button>
        </div>

        <p v-if="apiError" class="mb-4 text-destructive">{{ apiError }}</p>

        <Card class="mb-6">
            <CardHeader>
                <CardTitle class="text-base">{{ t("wallets.members.title") }}</CardTitle>
            </CardHeader>
            <CardContent>
                <MemberList
                    :members="memberList"
                    :current-user-id="currentUserId"
                    :is-owner="isOwner"
                    @remove="handleRemove"
                />
            </CardContent>
        </Card>

        <!-- Debt section placeholder — Phase 2c -->
        <Card>
            <CardContent class="py-6 text-center text-sm text-muted-foreground">
                {{ t("wallets.members.debtSection") }}
            </CardContent>
        </Card>
    </div>
</template>
