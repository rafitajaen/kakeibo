import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Wallet {
    id: string;
    name: string;
    type: string;
    currency: string;
    balance: number;
    isArchived: boolean;
    createdAt: string;
}

export interface WalletMember {
    userId: string;
    email: string;
    isOwner: boolean;
    joinedAt: string;
}

export interface CreateWalletData {
    name: string;
    type: string;
}

export interface UpdateWalletData {
    name: string;
}

export const useWalletsStore = defineStore("wallets", () => {
    // State
    const wallets = ref<Wallet[]>([]);
    const currentWallet = ref<Wallet | null>(null);
    const members = ref<Map<string, WalletMember[]>>(new Map());
    const isLoading = ref(false);

    // Getters
    const personalWallets = computed(() =>
        wallets.value.filter((w) => w.type === "Personal" && !w.isArchived),
    );
    const sharedWallets = computed(() =>
        wallets.value.filter((w) => w.type === "Shared" && !w.isArchived),
    );
    const archivedWallets = computed(() => wallets.value.filter((w) => w.isArchived));
    const activeWallets = computed(() => wallets.value.filter((w) => !w.isArchived));

    // Fetches the wallet list. Pass includeArchived=true to include archived wallets.
    async function fetchWallets(includeArchived = false): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<Wallet[]>("/api/wallets", {
                params: { includeArchived },
            });
            wallets.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Fetches a single wallet and sets it as the current wallet.
    async function fetchWallet(id: string): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<Wallet>(`/api/wallets/${id}`);
            currentWallet.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    // Creates a new wallet and adds it to the local list.
    async function createWallet(data: CreateWalletData): Promise<Wallet> {
        const response = await api.post<Wallet>("/api/wallets", data);
        wallets.value.push(response.data);
        return response.data;
    }

    // Updates an existing wallet's name and refreshes the local list entry.
    async function updateWallet(id: string, data: UpdateWalletData): Promise<Wallet> {
        const response = await api.put<Wallet>(`/api/wallets/${id}`, data);
        const index = wallets.value.findIndex((w) => w.id === id);
        if (index !== -1) {
            wallets.value[index] = response.data;
        }
        if (currentWallet.value?.id === id) {
            currentWallet.value = response.data;
        }
        return response.data;
    }

    // Archives (soft-deletes) a wallet and removes it from the active list.
    async function archiveWallet(id: string): Promise<void> {
        await api.delete(`/api/wallets/${id}`);
        wallets.value = wallets.value.filter((w) => w.id !== id);
        if (currentWallet.value?.id === id) {
            currentWallet.value = null;
        }
    }

    // Fetches the member list for a shared wallet and caches it by wallet ID.
    async function fetchMembers(walletId: string): Promise<WalletMember[]> {
        const response = await api.get<WalletMember[]>(`/api/wallets/${walletId}/members`);
        members.value.set(walletId, response.data);
        return response.data;
    }

    // Sends an invitation email to the given address for a shared wallet.
    async function inviteMember(walletId: string, email: string): Promise<void> {
        await api.post(`/api/wallets/${walletId}/invite`, { email });
    }

    // Accepts a wallet invitation by code; returns the wallet ID from the response.
    async function acceptInvitation(code: string): Promise<void> {
        await api.post(`/api/wallets/invitations/${code}/accept`);
    }

    // Removes a member from a shared wallet (leave or owner-kick).
    async function removeMember(walletId: string, userId: string): Promise<void> {
        await api.delete(`/api/wallets/${walletId}/members/${userId}`);
        const cached = members.value.get(walletId);
        if (cached) {
            members.value.set(
                walletId,
                cached.filter((m) => m.userId !== userId),
            );
        }
    }

    return {
        wallets,
        currentWallet,
        members,
        isLoading,
        personalWallets,
        sharedWallets,
        archivedWallets,
        activeWallets,
        fetchWallets,
        fetchWallet,
        createWallet,
        updateWallet,
        archiveWallet,
        fetchMembers,
        inviteMember,
        acceptInvitation,
        removeMember,
    };
});
