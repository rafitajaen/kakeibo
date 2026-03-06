<script setup lang="ts">
import { computed } from "vue";
import * as LucideIcons from "lucide-vue-next";
import { Wallet, Check } from "lucide-vue-next";
import { Badge } from "@/components/ui/badge";
import { useWalletsStore } from "@/stores/wallets";

const props = defineProps<{
    modelValue: string | string[] | null;
    multiple?: boolean;
}>();

const emit = defineEmits<{
    (e: "update:modelValue", value: string | string[] | null): void;
}>();

const walletsStore = useWalletsStore();
const activeWallets = computed(() => walletsStore.activeWallets);

function getIcon(name: string | null) {
    if (!name) return Wallet;
    return (LucideIcons as Record<string, unknown>)[name] ?? Wallet;
}

function isSelected(id: string): boolean {
    if (!props.modelValue) return false;
    if (Array.isArray(props.modelValue)) return props.modelValue.includes(id);
    return props.modelValue === id;
}

function isAllSelected(): boolean {
    return props.multiple && !props.modelValue;
}

function toggle(id: string) {
    if (!props.multiple) {
        emit("update:modelValue", isSelected(id) ? null : id);
        return;
    }
    const current = Array.isArray(props.modelValue) ? [...props.modelValue] : [];
    if (current.includes(id)) {
        const next = current.filter((x) => x !== id);
        emit("update:modelValue", next.length ? next : null);
    } else {
        emit("update:modelValue", [...current, id]);
    }
}

function selectAll() {
    emit("update:modelValue", null);
}
</script>

<template>
    <div class="space-y-1">
        <!-- "All wallets" option for multi-select -->
        <button
            v-if="multiple"
            type="button"
            class="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors hover:bg-muted"
            :class="isAllSelected() ? 'bg-muted font-medium' : ''"
            @click="selectAll"
        >
            <div class="flex size-8 items-center justify-center rounded-md bg-muted">
                <Wallet class="size-4" />
            </div>
            <span class="flex-1 text-left">All wallets</span>
            <Check v-if="isAllSelected()" class="size-4 text-primary" />
        </button>

        <button
            v-for="wallet in activeWallets"
            :key="wallet.id"
            type="button"
            class="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors hover:bg-muted"
            :class="isSelected(wallet.id) ? 'bg-muted font-medium' : ''"
            @click="toggle(wallet.id)"
        >
            <!-- Wallet icon with its background color -->
            <div
                class="flex size-8 items-center justify-center rounded-md shrink-0"
                :style="
                    wallet.backgroundColor
                        ? {
                              backgroundColor: wallet.backgroundColor,
                              color: wallet.textColor ?? '#fff',
                          }
                        : {}
                "
                :class="!wallet.backgroundColor ? 'bg-muted' : ''"
            >
                <component :is="getIcon(wallet.icon)" class="size-4" />
            </div>
            <div class="flex flex-1 flex-col items-start min-w-0">
                <span class="truncate">{{ wallet.name }}</span>
            </div>
            <Badge variant="secondary" class="shrink-0 text-xs">
                {{ wallet.type }}
            </Badge>
            <Check v-if="isSelected(wallet.id)" class="size-4 text-primary shrink-0" />
        </button>
    </div>
</template>
