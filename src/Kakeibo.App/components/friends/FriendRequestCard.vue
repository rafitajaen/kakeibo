<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Check, X } from "lucide-vue-next";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import type { FriendRequest } from "@/stores/friends";

const props = defineProps<{
    request: FriendRequest;
}>();

const emit = defineEmits<{
    accept: [requestId: string];
    reject: [requestId: string];
    viewProfile: [userId: string];
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
    <Card>
        <CardContent class="flex items-center gap-3 py-3">
            <button
                class="flex items-center gap-3 flex-1 text-left"
                @click="emit('viewProfile', request.senderUserId)"
            >
                <Avatar class="size-10">
                    <AvatarImage v-if="request.senderAvatarUrl" :src="request.senderAvatarUrl" />
                    <AvatarFallback>{{
                        getInitials(request.senderName, request.senderUsername)
                    }}</AvatarFallback>
                </Avatar>
                <div class="flex-1 min-w-0">
                    <p class="font-medium truncate">
                        {{ request.senderName ?? request.senderUsername }}
                    </p>
                    <p class="text-xs text-muted-foreground">@{{ request.senderUsername }}</p>
                </div>
            </button>
            <div class="flex gap-2">
                <Button size="sm" @click="emit('accept', request.id)">
                    <Check class="size-4" />
                </Button>
                <Button size="sm" variant="outline" @click="emit('reject', request.id)">
                    <X class="size-4" />
                </Button>
            </div>
        </CardContent>
    </Card>
</template>
