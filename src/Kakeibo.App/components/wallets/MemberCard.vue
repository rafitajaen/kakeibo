<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { WalletMember } from "@/stores/wallets";

const props = defineProps<{
    member: WalletMember;
    // The authenticated user's ID — used to show "Leave" vs "Remove" label.
    currentUserId: string;
    // Whether the current user is the wallet owner (can remove others).
    isOwner: boolean;
}>();

const emit = defineEmits<{
    (e: "remove", userId: string): void;
}>();

const { t } = useI18n();

function handleRemove() {
    const isself = props.member.userId === props.currentUserId;
    const message = isself ? t("wallets.members.leaveConfirm") : t("wallets.members.removeConfirm");
    if (!confirm(message)) return;
    emit("remove", props.member.userId);
}

// Returns true if the remove/leave button should be visible.
function canRemove(): boolean {
    if (props.member.isOwner) return false;
    // Members can always leave; owners can remove non-owner members.
    return props.member.userId === props.currentUserId || props.isOwner;
}
</script>

<template>
    <div class="flex items-center justify-between rounded-md border px-4 py-3">
        <div class="flex flex-col gap-0.5">
            <div class="flex items-center gap-2">
                <span class="text-sm font-medium">{{ member.email }}</span>
                <Badge v-if="member.isOwner" variant="secondary" class="text-xs">
                    {{ t("wallets.members.owner") }}
                </Badge>
            </div>
            <span class="text-xs text-muted-foreground">
                {{ t("wallets.members.joinedAt") }}
                {{ new Date(member.joinedAt).toLocaleDateString() }}
            </span>
        </div>

        <Button
            v-if="canRemove()"
            variant="ghost"
            size="sm"
            class="text-destructive hover:text-destructive"
            @click="handleRemove"
        >
            {{
                member.userId === currentUserId
                    ? t("wallets.members.removeSelf")
                    : t("wallets.members.removeMember")
            }}
        </Button>
    </div>
</template>
