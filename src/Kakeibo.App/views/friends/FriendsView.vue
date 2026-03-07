<script setup lang="ts">
import { ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Search, UserPlus, Users } from "lucide-vue-next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import FriendCard from "@/components/friends/FriendCard.vue";
import UserSearchResults from "@/components/friends/UserSearchResults.vue";
import { useFriendsStore } from "@/stores/friends";

const { t } = useI18n();
const router = useRouter();
const friendsStore = useFriendsStore();

const searchQuery = ref("");
const searchTimeout = ref<ReturnType<typeof setTimeout> | null>(null);

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
    await friendsStore.deleteFriendship(friendshipId);
}

async function handleSendRequest(userId: string) {
    await friendsStore.sendFriendRequest(userId);
    searchQuery.value = "";
    friendsStore.searchResults = [];
}
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
</template>
