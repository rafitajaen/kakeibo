<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import type { WalletMember } from "@/stores/wallets";

const props = defineProps<{
    member: WalletMember;
    // The authenticated user's ID — used to show "Leave" vs "Remove" label.
    currentUserId: string;
    // The authenticated user's role in the wallet ("Owner", "Editor", "Guest").
    callerRole: string;
}>();

const emit = defineEmits<{
    (e: "remove", userId: string): void;
    (e: "roleChange", userId: string, role: string): void;
}>();

const { t } = useI18n();
const isChangingRole = ref(false);

function handleRemove() {
    const isSelf = props.member.userId === props.currentUserId;
    const message = isSelf ? t("wallets.members.leaveConfirm") : t("wallets.members.removeConfirm");
    if (!confirm(message)) return;
    emit("remove", props.member.userId);
}

function handleRoleChange(newRole: string) {
    isChangingRole.value = true;
    emit("roleChange", props.member.userId, newRole);
}

// Returns true if the caller (Owner) can change this member's role.
function canChangeRole(): boolean {
    return (
        props.callerRole === "Owner" &&
        props.member.userId !== props.currentUserId &&
        props.member.role !== "Owner"
    );
}

// Returns true if the remove/leave button should be visible.
function canRemove(): boolean {
    if (props.member.role === "Owner") return false;
    return props.member.userId === props.currentUserId || props.callerRole === "Owner";
}
</script>

<template>
    <div class="flex items-center justify-between rounded-md border px-4 py-3">
        <div class="flex flex-col gap-0.5">
            <div class="flex items-center gap-2">
                <span class="text-sm font-medium">{{ member.email }}</span>
                <Badge variant="secondary" class="text-xs">
                    {{ t(`wallets.members.role.${member.role.toLowerCase()}`) }}
                </Badge>
            </div>
            <span class="text-xs text-muted-foreground">
                {{ t("wallets.members.joinedAt") }}
                {{ new Date(member.joinedAt).toLocaleDateString() }}
            </span>
        </div>

        <div class="flex items-center gap-2">
            <!-- Role selector for Owner to change Editor/Guest roles -->
            <Select
                v-if="canChangeRole()"
                :model-value="member.role"
                :disabled="isChangingRole"
                @update:model-value="handleRoleChange"
            >
                <SelectTrigger class="h-8 w-28 text-xs">
                    <SelectValue />
                </SelectTrigger>
                <SelectContent>
                    <SelectItem value="Editor">
                        {{ t("wallets.members.role.editor") }}
                    </SelectItem>
                    <SelectItem value="Guest">
                        {{ t("wallets.members.role.guest") }}
                    </SelectItem>
                </SelectContent>
            </Select>

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
    </div>
</template>
