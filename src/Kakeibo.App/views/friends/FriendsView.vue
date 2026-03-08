<script setup lang="ts">
import { ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Search, UserPlus, Users, AlertTriangle } from "lucide-vue-next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import FriendCard from "@/components/friends/FriendCard.vue";
import UserSearchResults from "@/components/friends/UserSearchResults.vue";
import { useFriendsStore, type FriendshipImpact } from "@/stores/friends";

const { t } = useI18n();
const router = useRouter();
const friendsStore = useFriendsStore();

const searchQuery = ref("");
const searchTimeout = ref<ReturnType<typeof setTimeout> | null>(null);

// Dialog state
const showDeleteDialog = ref(false);
const pendingDeleteId = ref<string | null>(null);
const pendingDeleteName = ref("");
const friendshipImpact = ref<FriendshipImpact | null>(null);
const isCheckingImpact = ref(false);

onMounted(async () => {
    await Promise.all([friendsStore.fetchFriends(), friendsStore.fetchReceivedRequests()]);
});

watch(searchQuery, (query) => {
    if (searchTimeout.value) clearTimeout(searchTimeout.value);
    searchTimeout.value = setTimeout(() => {
        friendsStore.searchUsers(query);
    }, 300);
});

async function handleDeleteFriendship(friendshipId: string) {
    const friend = friendsStore.friends.find((f) => f.friendshipId === friendshipId);
    pendingDeleteId.value = friendshipId;
    pendingDeleteName.value = friend?.name ?? friend?.username ?? "";

    isCheckingImpact.value = true;
    try {
        friendshipImpact.value = await friendsStore.checkFriendshipImpact(friendshipId);
    } finally {
        isCheckingImpact.value = false;
    }

    showDeleteDialog.value = true;
}

async function confirmDeleteFriendship() {
    if (!pendingDeleteId.value) return;
    await friendsStore.deleteFriendship(pendingDeleteId.value);
    showDeleteDialog.value = false;
    pendingDeleteId.value = null;
    friendshipImpact.value = null;
}

function cancelDeleteFriendship() {
    showDeleteDialog.value = false;
    pendingDeleteId.value = null;
    friendshipImpact.value = null;
}

async function handleSendRequest(userId: string) {
    await friendsStore.sendFriendRequest(userId);
    searchQuery.value = "";
    friendsStore.clearSearchResults();
}

const hasLostAccess = (impact: FriendshipImpact) =>
    impact.sharedWallets.some((w) => w.willLoseAccess);
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-3xl">
        <div class="flex items-center justify-between mb-6">
            <h1 class="text-2xl font-bold">{{ t("friends.title") }}</h1>
            <Button variant="outline" @click="router.push({ name: 'friend-requests' })">
                <UserPlus class="mr-2 size-4" />
                {{ t("friends.requests") }}
                <Badge
                    v-if="friendsStore.pendingRequestCount > 0"
                    variant="destructive"
                    class="ml-2"
                >
                    {{ friendsStore.pendingRequestCount }}
                </Badge>
            </Button>
        </div>

        <!-- Search users -->
        <Card class="mb-6">
            <CardHeader>
                <CardTitle class="text-base">{{ t("friends.searchUsers") }}</CardTitle>
            </CardHeader>
            <CardContent>
                <div class="relative">
                    <Search
                        class="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground"
                    />
                    <Input
                        v-model="searchQuery"
                        :placeholder="t('friends.searchPlaceholder')"
                        class="pl-9"
                    />
                </div>
                <UserSearchResults
                    v-if="friendsStore.searchResults.length > 0"
                    :results="friendsStore.searchResults"
                    class="mt-3"
                    @send-request="handleSendRequest"
                    @view-profile="(id) => router.push({ name: 'user-profile', params: { id } })"
                />
            </CardContent>
        </Card>

        <!-- Friends list -->
        <div v-if="friendsStore.isLoading" class="text-center py-8 text-muted-foreground">
            {{ t("common.loading") }}
        </div>

        <div
            v-else-if="friendsStore.friends.length === 0"
            class="text-center py-12 text-muted-foreground"
        >
            <Users class="mx-auto mb-4 size-12 opacity-50" />
            <p>{{ t("friends.empty") }}</p>
        </div>

        <div v-else class="space-y-3">
            <FriendCard
                v-for="friend in friendsStore.friends"
                :key="friend.friendshipId"
                :friend="friend"
                @view-profile="(id) => router.push({ name: 'user-profile', params: { id } })"
                @delete="handleDeleteFriendship"
            />
        </div>
    </div>

    <!-- Remove friend confirmation dialog -->
    <AlertDialog :open="showDeleteDialog" @update:open="(v) => !v && cancelDeleteFriendship()">
        <AlertDialogContent>
            <AlertDialogHeader>
                <AlertDialogTitle class="flex items-center gap-2">
                    <AlertTriangle
                        v-if="friendshipImpact && friendshipImpact.sharedWallets.length > 0"
                        class="size-5 text-amber-500"
                    />
                    {{ t("friends.removeFriendDialog.title") }}
                </AlertDialogTitle>
                <AlertDialogDescription>
                    {{ t("friends.removeFriendDialog.description", { name: pendingDeleteName }) }}
                </AlertDialogDescription>
            </AlertDialogHeader>

            <!-- Shared wallets warning -->
            <div
                v-if="friendshipImpact && friendshipImpact.sharedWallets.length > 0"
                class="space-y-3"
            >
                <p class="text-sm font-medium">
                    {{ t("friends.removeFriendDialog.sharedWalletsWarning") }}
                </p>

                <p
                    v-if="hasLostAccess(friendshipImpact)"
                    class="text-sm text-amber-600 dark:text-amber-400"
                >
                    {{ t("friends.removeFriendDialog.loseAccessNote") }}
                </p>

                <ul class="space-y-1.5">
                    <li
                        v-for="wallet in friendshipImpact.sharedWallets"
                        :key="wallet.id"
                        class="flex items-center justify-between rounded-md border px-3 py-2 text-sm"
                        :class="
                            wallet.willLoseAccess
                                ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/30'
                                : ''
                        "
                    >
                        <span class="font-medium">{{ wallet.name }}</span>
                        <div class="flex items-center gap-2 text-muted-foreground">
                            <span>{{
                                t("friends.removeFriendDialog.roleLabel", { role: wallet.yourRole })
                            }}</span>
                            <Badge
                                v-if="wallet.willLoseAccess"
                                variant="destructive"
                                class="text-xs"
                            >
                                {{ t("friends.removeFriendDialog.loseAccess") }}
                            </Badge>
                        </div>
                    </li>
                </ul>
            </div>

            <AlertDialogFooter>
                <AlertDialogCancel @click="cancelDeleteFriendship">
                    {{ t("friends.removeFriendDialog.cancel") }}
                </AlertDialogCancel>
                <AlertDialogAction
                    class="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    @click="confirmDeleteFriendship"
                >
                    {{ t("friends.removeFriendDialog.confirm") }}
                </AlertDialogAction>
            </AlertDialogFooter>
        </AlertDialogContent>
    </AlertDialog>
</template>
