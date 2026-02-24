<script setup lang="ts">
import { useI18n } from "vue-i18n";
import MemberCard from "@/components/wallets/MemberCard.vue";
import type { WalletMember } from "@/stores/wallets";

defineProps<{
    members: WalletMember[];
    currentUserId: string;
    isOwner: boolean;
}>();

const emit = defineEmits<{
    (e: "remove", userId: string): void;
}>();

const { t } = useI18n();
</script>

<template>
    <div class="space-y-2">
        <p v-if="members.length === 0" class="text-sm text-muted-foreground">
            {{ t("wallets.members.empty") }}
        </p>
        <MemberCard
            v-for="member in members"
            :key="member.userId"
            :member="member"
            :current-user-id="currentUserId"
            :is-owner="isOwner"
            @remove="emit('remove', $event)"
        />
    </div>
</template>
