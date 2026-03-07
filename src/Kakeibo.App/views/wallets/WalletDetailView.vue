<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useWalletsStore } from "@/stores/wallets";
import { useFriendsStore } from "@/stores/friends";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const walletsStore = useWalletsStore();
const friendsStore = useFriendsStore();

const apiError = ref<string | null>(null);
const visibilityError = ref<string | null>(null);
const isUpdatingVisibility = ref(false);

// Transfer dialog state
const showTransferDialog = ref(false);
const transferTargetId = ref<string>("");
const isTransferring = ref(false);
const transferError = ref<string | null>(null);

// Make-private dialog state
const showMakePrivateDialog = ref(false);
const makePrivatePreview = ref<{ membersToRemove: string[]; message: string } | null>(null);
const isLoadingPreview = ref(false);
const isMakingPrivate = ref(false);
const makePrivateError = ref<string | null>(null);

onMounted(async () => {
    try {
        await walletsStore.fetchWallet(route.params.id as string);
        // Pre-load friends so the transfer dialog is ready.
        await friendsStore.fetchFriends();
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

async function handleVisibilityChange(value: string) {
    visibilityError.value = null;
    isUpdatingVisibility.value = true;
    try {
        await walletsStore.updateVisibility(route.params.id as string, value);
    } catch {
        visibilityError.value = t("wallets.errors.unexpected");
    } finally {
        isUpdatingVisibility.value = false;
    }
}

async function openMakePrivateDialog() {
    showMakePrivateDialog.value = true;
    isLoadingPreview.value = true;
    makePrivateError.value = null;
    try {
        makePrivatePreview.value = await walletsStore.previewMakePrivate(route.params.id as string);
    } catch {
        makePrivateError.value = t("wallets.errors.unexpected");
    } finally {
        isLoadingPreview.value = false;
    }
}

async function handleMakePrivate() {
    isMakingPrivate.value = true;
    makePrivateError.value = null;
    try {
        await walletsStore.makeWalletPrivate(route.params.id as string);
        showMakePrivateDialog.value = false;
        await router.push({ name: "wallets" });
    } catch {
        makePrivateError.value = t("wallets.errors.unexpected");
    } finally {
        isMakingPrivate.value = false;
    }
}

async function handleTransfer() {
    if (!transferTargetId.value) return;
    isTransferring.value = true;
    transferError.value = null;
    try {
        await walletsStore.transferWallet(route.params.id as string, transferTargetId.value);
        showTransferDialog.value = false;
        await router.push({ name: "wallets" });
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 400) {
            transferError.value = t("wallets.transfer.errorNotFriends");
        } else {
            transferError.value = t("wallets.errors.unexpected");
        }
    } finally {
        isTransferring.value = false;
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

                <CardContent class="space-y-4">
                    <div class="flex items-baseline gap-2">
                        <span class="text-3xl font-semibold tabular-nums">
                            {{ walletsStore.currentWallet.balance.toFixed(2) }}
                        </span>
                        <span class="text-muted-foreground">{{
                            walletsStore.currentWallet.currency
                        }}</span>
                    </div>

                    <!-- Visibility selector — only for wallet Owners -->
                    <div v-if="walletsStore.currentWallet.callerRole === 'Owner'" class="space-y-1">
                        <Label>{{ t("wallets.visibility.label") }}</Label>
                        <Select
                            :model-value="walletsStore.currentWallet.visibility"
                            :disabled="isUpdatingVisibility"
                            @update:model-value="handleVisibilityChange"
                        >
                            <SelectTrigger class="w-40">
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="Private">
                                    {{ t("wallets.visibility.private") }}
                                </SelectItem>
                                <SelectItem value="Public">
                                    {{ t("wallets.visibility.public") }}
                                </SelectItem>
                            </SelectContent>
                        </Select>
                        <p v-if="visibilityError" class="text-sm text-destructive">
                            {{ visibilityError }}
                        </p>
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

                    <!-- Owner-only: Transfer personal wallet -->
                    <Button
                        v-if="
                            walletsStore.currentWallet.callerRole === 'Owner' &&
                            walletsStore.currentWallet.type === 'Personal'
                        "
                        variant="outline"
                        @click="
                            transferTargetId = '';
                            transferError = null;
                            showTransferDialog = true;
                        "
                    >
                        {{ t("wallets.transfer.action") }}
                    </Button>

                    <!-- Owner-only: Convert shared wallet to private -->
                    <Button
                        v-if="
                            walletsStore.currentWallet.callerRole === 'Owner' &&
                            walletsStore.currentWallet.type === 'Shared'
                        "
                        variant="outline"
                        @click="openMakePrivateDialog"
                    >
                        {{ t("wallets.makePrivate.action") }}
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

        <!-- Transfer wallet dialog -->
        <Dialog v-model:open="showTransferDialog">
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{{ t("wallets.transfer.title") }}</DialogTitle>
                    <DialogDescription>{{ t("wallets.transfer.description") }}</DialogDescription>
                </DialogHeader>

                <div class="space-y-2 py-2">
                    <Label>{{ t("wallets.transfer.selectFriend") }}</Label>
                    <Select v-model="transferTargetId">
                        <SelectTrigger>
                            <SelectValue :placeholder="t('wallets.transfer.selectPlaceholder')" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem
                                v-for="friend in friendsStore.friends"
                                :key="friend.userId"
                                :value="friend.userId"
                            >
                                {{ friend.name ?? friend.username }}
                            </SelectItem>
                        </SelectContent>
                    </Select>
                    <p v-if="transferError" class="text-sm text-destructive">
                        {{ transferError }}
                    </p>
                </div>

                <DialogFooter>
                    <Button variant="outline" @click="showTransferDialog = false">
                        {{ t("common.cancel") }}
                    </Button>
                    <Button
                        :disabled="!transferTargetId || isTransferring"
                        class="text-destructive-foreground bg-destructive hover:bg-destructive/90"
                        @click="handleTransfer"
                    >
                        {{ isTransferring ? t("common.saving") : t("wallets.transfer.confirm") }}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>

        <!-- Make wallet private dialog -->
        <Dialog v-model:open="showMakePrivateDialog">
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{{ t("wallets.makePrivate.title") }}</DialogTitle>
                    <DialogDescription>
                        {{ t("wallets.makePrivate.description") }}
                    </DialogDescription>
                </DialogHeader>

                <div class="py-2">
                    <p v-if="isLoadingPreview" class="text-sm text-muted-foreground">
                        {{ t("common.loading") }}
                    </p>
                    <template v-else-if="makePrivatePreview">
                        <p class="mb-2 text-sm">{{ makePrivatePreview.message }}</p>
                        <ul
                            v-if="makePrivatePreview.membersToRemove.length > 0"
                            class="space-y-1 text-sm text-muted-foreground"
                        >
                            <li
                                v-for="email in makePrivatePreview.membersToRemove"
                                :key="email"
                                class="flex items-center gap-2"
                            >
                                <span class="h-1.5 w-1.5 rounded-full bg-destructive" />
                                {{ email }}
                            </li>
                        </ul>
                    </template>
                    <p v-if="makePrivateError" class="mt-2 text-sm text-destructive">
                        {{ makePrivateError }}
                    </p>
                </div>

                <DialogFooter>
                    <Button variant="outline" @click="showMakePrivateDialog = false">
                        {{ t("common.cancel") }}
                    </Button>
                    <Button
                        :disabled="isLoadingPreview || isMakingPrivate"
                        class="text-destructive-foreground bg-destructive hover:bg-destructive/90"
                        @click="handleMakePrivate"
                    >
                        {{
                            isMakingPrivate ? t("common.saving") : t("wallets.makePrivate.confirm")
                        }}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    </div>
</template>
