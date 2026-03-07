<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { UserPlus, UserCheck } from "lucide-vue-next";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useFriendsStore, type UserProfile } from "@/stores/friends";

const { t } = useI18n();
const route = useRoute();
const friendsStore = useFriendsStore();

const profile = ref<UserProfile | null>(null);
const isLoading = ref(true);
const requestSent = ref(false);

onMounted(async () => {
    const userId = route.params.id as string;
    try {
        profile.value = await friendsStore.getUserProfile(userId);
    } finally {
        isLoading.value = false;
    }
});

function getInitials(name: string | null, username: string): string {
    if (name) {
        return name
            .split(" ")
            .map((s) => s[0])
            .join("")
            .toUpperCase()
            .slice(0, 2);
    }
    return username[0].toUpperCase();
}

async function handleSendRequest() {
    if (!profile.value) return;
    await friendsStore.sendFriendRequest(profile.value.id);
    requestSent.value = true;
}
</script>

<template>
    <div class="container mx-auto px-4 py-8 max-w-lg">
        <div v-if="isLoading" class="text-center py-8 text-muted-foreground">
            {{ t("common.loading") }}
        </div>

        <Card v-else-if="profile">
            <CardHeader class="items-center text-center">
                <Avatar class="size-20 mb-3">
                    <AvatarImage v-if="profile.avatarUrl" :src="profile.avatarUrl" />
                    <AvatarFallback class="text-xl">
                        {{ getInitials(profile.name, profile.username) }}
                    </AvatarFallback>
                </Avatar>
                <CardTitle>{{ profile.name ?? profile.username }}</CardTitle>
                <p class="text-sm text-muted-foreground">@{{ profile.username }}</p>
            </CardHeader>
            <CardContent class="text-center">
                <div
                    v-if="profile.isFriend"
                    class="flex items-center justify-center gap-2 text-sm text-muted-foreground"
                >
                    <UserCheck class="size-4" />
                    {{ t("friends.friendsSince") }}
                </div>
                <Button v-else-if="requestSent" variant="outline" disabled class="w-full mt-4">
                    {{ t("friends.requestSent") }}
                </Button>
                <Button v-else class="w-full mt-4" @click="handleSendRequest">
                    <UserPlus class="mr-2 size-4" />
                    {{ t("friends.addFriend") }}
                </Button>
            </CardContent>
        </Card>
    </div>
</template>
