<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { UserPlus } from "lucide-vue-next";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import type { UserSearchResult } from "@/stores/friends";

const props = defineProps<{
    results: UserSearchResult[];
}>();

const emit = defineEmits<{
    sendRequest: [userId: string];
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
    <div class="border rounded-md divide-y">
        <div
            v-for="user in results"
            :key="user.id"
            class="flex items-center gap-3 p-3 hover:bg-muted/50 transition-colors"
        >
            <button
                class="flex items-center gap-3 flex-1 text-left"
                @click="emit('viewProfile', user.id)"
            >
                <Avatar class="size-8">
                    <AvatarImage v-if="user.avatarUrl" :src="user.avatarUrl" />
                    <AvatarFallback class="text-xs">{{
                        getInitials(user.name, user.username)
                    }}</AvatarFallback>
                </Avatar>
                <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium truncate">{{ user.name ?? user.username }}</p>
                    <p class="text-xs text-muted-foreground">@{{ user.username }}</p>
                </div>
            </button>
            <Button size="sm" variant="outline" @click="emit('sendRequest', user.id)">
                <UserPlus class="size-4" />
            </Button>
        </div>
    </div>
</template>
