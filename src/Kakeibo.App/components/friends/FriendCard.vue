<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { UserMinus } from "lucide-vue-next";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import type { Friend } from "@/stores/friends";

const props = defineProps<{
    friend: Friend;
}>();

const emit = defineEmits<{
    viewProfile: [userId: string];
    delete: [friendshipId: string];
}>();

const { t } = useI18n();

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
</script>

<template>
    <Card class="hover:bg-muted/50 transition-colors">
        <CardContent class="flex items-center gap-3 py-3">
            <button
                class="flex items-center gap-3 flex-1 text-left"
                @click="emit('viewProfile', friend.userId)"
            >
                <Avatar class="size-10">
                    <AvatarImage v-if="friend.avatarUrl" :src="friend.avatarUrl" />
                    <AvatarFallback>{{ getInitials(friend.name, friend.username) }}</AvatarFallback>
                </Avatar>
                <div class="flex-1 min-w-0">
                    <p class="font-medium truncate">{{ friend.name ?? friend.username }}</p>
                    <p class="text-xs text-muted-foreground">@{{ friend.username }}</p>
                </div>
            </button>
            <Button
                variant="ghost"
                size="icon"
                class="text-muted-foreground hover:text-destructive"
                :title="t('friends.removeFriend')"
                @click="emit('delete', friend.friendshipId)"
            >
                <UserMinus class="size-4" />
            </Button>
        </CardContent>
    </Card>
</template>
