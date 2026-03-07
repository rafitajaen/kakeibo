<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { X } from "lucide-vue-next";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import type { SentRequest } from "@/stores/friends";

const props = defineProps<{
    request: SentRequest;
}>();

const emit = defineEmits<{
    cancel: [requestId: string];
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
                @click="emit('viewProfile', request.receiverUserId)"
            >
                <Avatar class="size-10">
                    <AvatarImage
                        v-if="request.receiverAvatarUrl"
                        :src="request.receiverAvatarUrl"
                    />
                    <AvatarFallback>{{
                        getInitials(request.receiverName, request.receiverUsername)
                    }}</AvatarFallback>
                </Avatar>
                <div class="flex-1 min-w-0">
                    <p class="font-medium truncate">
                        {{ request.receiverName ?? request.receiverUsername }}
                    </p>
                    <p class="text-xs text-muted-foreground">@{{ request.receiverUsername }}</p>
                </div>
            </button>
            <Button size="sm" variant="outline" @click="emit('cancel', request.id)">
                <X class="mr-1 size-4" />
                {{ t("common.cancel") }}
            </Button>
        </CardContent>
    </Card>
</template>
