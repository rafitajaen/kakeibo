<script setup lang="ts">
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import FriendRequestCard from "@/components/friends/FriendRequestCard.vue";
import SentRequestCard from "@/components/friends/SentRequestCard.vue";
import { useFriendsStore } from "@/stores/friends";

const { t } = useI18n();
const router = useRouter();
const friendsStore = useFriendsStore();

onMounted(async () => {
    await Promise.all([friendsStore.fetchReceivedRequests(), friendsStore.fetchSentRequests()]);
});

async function handleAccept(requestId: string) {
    await friendsStore.acceptRequest(requestId);
}

async function handleReject(requestId: string) {
    await friendsStore.rejectRequest(requestId);
}

async function handleCancel(requestId: string) {
    await friendsStore.cancelRequest(requestId);
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-3xl">
        <h1 class="text-2xl font-bold mb-6">{{ t("friends.requests") }}</h1>

        <Tabs default-value="received">
            <TabsList class="mb-4">
                <TabsTrigger value="received">
                    {{ t("friends.received") }}
                    <Badge
                        v-if="friendsStore.receivedRequests.length > 0"
                        variant="destructive"
                        class="ml-2"
                    >
                        {{ friendsStore.receivedRequests.length }}
                    </Badge>
                </TabsTrigger>
                <TabsTrigger value="sent">
                    {{ t("friends.sent") }}
                </TabsTrigger>
            </TabsList>

            <TabsContent value="received">
                <div
                    v-if="friendsStore.receivedRequests.length === 0"
                    class="text-center py-8 text-muted-foreground"
                >
                    {{ t("friends.noReceivedRequests") }}
                </div>
                <div v-else class="space-y-3">
                    <FriendRequestCard
                        v-for="request in friendsStore.receivedRequests"
                        :key="request.id"
                        :request="request"
                        @accept="handleAccept"
                        @reject="handleReject"
                        @view-profile="
                            (id) => router.push({ name: 'user-profile', params: { id } })
                        "
                    />
                </div>
            </TabsContent>

            <TabsContent value="sent">
                <div
                    v-if="friendsStore.sentRequests.length === 0"
                    class="text-center py-8 text-muted-foreground"
                >
                    {{ t("friends.noSentRequests") }}
                </div>
                <div v-else class="space-y-3">
                    <SentRequestCard
                        v-for="request in friendsStore.sentRequests"
                        :key="request.id"
                        :request="request"
                        @cancel="handleCancel"
                        @view-profile="
                            (id) => router.push({ name: 'user-profile', params: { id } })
                        "
                    />
                </div>
            </TabsContent>
        </Tabs>
    </div>
</template>
