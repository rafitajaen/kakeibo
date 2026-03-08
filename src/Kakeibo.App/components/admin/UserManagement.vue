<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useAdminStore } from "@/stores/admin";
import type { AdminUser } from "@/stores/admin";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { MoreHorizontal, Search } from "lucide-vue-next";

const { t } = useI18n();
const admin = useAdminStore();

const search = ref("");
const confirmDelete = ref<AdminUser | null>(null);
const confirmBlock = ref<AdminUser | null>(null);

onMounted(() => fetchUsers());

async function fetchUsers() {
    await admin.fetchUsers(1, 50, search.value || undefined);
}

async function handleBlock(user: AdminUser) {
    if (user.isBlocked) {
        await admin.unblockUser(user.id);
    } else {
        confirmBlock.value = user;
    }
}

async function confirmBlockAction() {
    if (!confirmBlock.value) return;
    await admin.blockUser(confirmBlock.value.id);
    confirmBlock.value = null;
}

async function handleDelete(user: AdminUser) {
    confirmDelete.value = user;
}

async function confirmDeleteAction() {
    if (!confirmDelete.value) return;
    await admin.deleteUser(confirmDelete.value.id);
    confirmDelete.value = null;
}
</script>

<template>
    <div class="space-y-4">
        <!-- Search bar -->
        <div class="relative">
            <Search
                class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"
            />
            <Input
                v-model="search"
                :placeholder="t('admin.users.searchPlaceholder')"
                class="pl-9"
                @keyup.enter="fetchUsers"
            />
        </div>

        <!-- Loading skeleton -->
        <div v-if="admin.loadingUsers" class="space-y-2">
            <Skeleton v-for="i in 5" :key="i" class="h-14 w-full" />
        </div>

        <!-- User list -->
        <div v-else class="rounded-md border">
            <div class="divide-y">
                <div
                    v-for="user in admin.users"
                    :key="user.id"
                    class="flex items-center justify-between p-4"
                >
                    <div class="min-w-0 flex-1">
                        <div class="flex items-center gap-2">
                            <span class="font-medium truncate">{{ user.email }}</span>
                            <Badge v-if="user.role === 'Admin'" variant="default" class="text-xs">
                                Admin
                            </Badge>
                            <Badge v-if="user.isBlocked" variant="destructive" class="text-xs">
                                {{ t("admin.users.blocked") }}
                            </Badge>
                            <Badge v-if="!user.isVerified" variant="secondary" class="text-xs">
                                {{ t("admin.users.unverified") }}
                            </Badge>
                        </div>
                        <p class="text-sm text-muted-foreground">@{{ user.username }}</p>
                    </div>

                    <DropdownMenu>
                        <DropdownMenuTrigger as-child>
                            <Button variant="ghost" size="icon">
                                <MoreHorizontal class="h-4 w-4" />
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                            <DropdownMenuItem @click="handleBlock(user)">
                                {{
                                    user.isBlocked
                                        ? t("admin.users.unblock")
                                        : t("admin.users.block")
                                }}
                            </DropdownMenuItem>
                            <DropdownMenuSeparator v-if="user.role !== 'Admin'" />
                            <DropdownMenuItem
                                v-if="user.role !== 'Admin'"
                                class="text-destructive"
                                @click="handleDelete(user)"
                            >
                                {{ t("admin.users.delete") }}
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>

                <div v-if="!admin.users.length" class="p-8 text-center text-muted-foreground">
                    {{ t("admin.users.empty") }}
                </div>
            </div>
        </div>

        <!-- Confirm block dialog -->
        <AlertDialog :open="!!confirmBlock" @update:open="confirmBlock = null">
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>{{ t("admin.users.confirmBlockTitle") }}</AlertDialogTitle>
                    <AlertDialogDescription>
                        {{
                            t("admin.users.confirmBlockDescription", { email: confirmBlock?.email })
                        }}
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                    <AlertDialogAction @click="confirmBlockAction">
                        {{ t("admin.users.block") }}
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>

        <!-- Confirm delete dialog -->
        <AlertDialog :open="!!confirmDelete" @update:open="confirmDelete = null">
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>{{ t("admin.users.confirmDeleteTitle") }}</AlertDialogTitle>
                    <AlertDialogDescription>
                        {{
                            t("admin.users.confirmDeleteDescription", {
                                email: confirmDelete?.email,
                            })
                        }}
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                    <AlertDialogAction
                        class="bg-destructive text-destructive-foreground"
                        @click="confirmDeleteAction"
                    >
                        {{ t("admin.users.delete") }}
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    </div>
</template>
