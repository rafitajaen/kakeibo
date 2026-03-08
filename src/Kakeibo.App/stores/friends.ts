import { ref, computed } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export interface Friend {
    friendshipId: string;
    userId: string;
    username: string;
    name: string | null;
    avatarUrl: string | null;
    friendsSince: string;
}

export interface FriendRequest {
    id: string;
    senderUserId: string;
    senderUsername: string;
    senderName: string | null;
    senderAvatarUrl: string | null;
    sentAt: string;
}

export interface SentRequest {
    id: string;
    receiverUserId: string;
    receiverUsername: string;
    receiverName: string | null;
    receiverAvatarUrl: string | null;
    sentAt: string;
}

export interface UserSearchResult {
    id: string;
    username: string;
    name: string | null;
    avatarUrl: string | null;
}

export interface UserProfile {
    id: string;
    username: string;
    name: string | null;
    avatarUrl: string | null;
    isFriend: boolean;
    friendsSince: string | null;
}

export interface AffectedWallet {
    id: string;
    name: string;
    yourRole: string;
    willLoseAccess: boolean;
}

export interface FriendshipImpact {
    sharedWallets: AffectedWallet[];
    guestAccessRevoked: number;
}

export const useFriendsStore = defineStore("friends", () => {
    const friends = ref<Friend[]>([]);
    const receivedRequests = ref<FriendRequest[]>([]);
    const sentRequests = ref<SentRequest[]>([]);
    const searchResults = ref<UserSearchResult[]>([]);
    const isLoading = ref(false);

    const friendCount = computed(() => friends.value.length);
    const pendingRequestCount = computed(() => receivedRequests.value.length);

    async function fetchFriends(): Promise<void> {
        isLoading.value = true;
        try {
            const response = await api.get<Friend[]>("/api/friends");
            friends.value = response.data;
        } finally {
            isLoading.value = false;
        }
    }

    async function fetchReceivedRequests(): Promise<void> {
        const response = await api.get<FriendRequest[]>("/api/friends/requests");
        receivedRequests.value = response.data;
    }

    async function fetchSentRequests(): Promise<void> {
        const response = await api.get<SentRequest[]>("/api/friends/requests/sent");
        sentRequests.value = response.data;
    }

    async function sendFriendRequest(receiverUserId: string): Promise<void> {
        await api.post("/api/friends/requests", { receiverUserId });
        await fetchSentRequests();
    }

    async function acceptRequest(requestId: string): Promise<void> {
        await api.post(`/api/friends/requests/${requestId}/accept`);
        receivedRequests.value = receivedRequests.value.filter((r) => r.id !== requestId);
        await fetchFriends();
    }

    async function rejectRequest(requestId: string): Promise<void> {
        await api.post(`/api/friends/requests/${requestId}/reject`);
        receivedRequests.value = receivedRequests.value.filter((r) => r.id !== requestId);
    }

    async function cancelRequest(requestId: string): Promise<void> {
        await api.delete(`/api/friends/requests/${requestId}`);
        sentRequests.value = sentRequests.value.filter((r) => r.id !== requestId);
    }

    async function deleteFriendship(friendshipId: string): Promise<void> {
        await api.delete(`/api/friends/${friendshipId}`);
        friends.value = friends.value.filter((f) => f.friendshipId !== friendshipId);
    }

    function clearSearchResults(): void {
        searchResults.value = [];
    }

    async function searchUsers(query: string): Promise<void> {
        if (query.length < 2) {
            clearSearchResults();
            return;
        }
        const response = await api.get<UserSearchResult[]>("/api/users/search", {
            params: { q: query },
        });
        searchResults.value = response.data;
    }

    async function getUserProfile(userId: string): Promise<UserProfile> {
        const response = await api.get<UserProfile>(`/api/users/${userId}/profile`);
        return response.data;
    }

    async function checkFriendshipImpact(friendshipId: string): Promise<FriendshipImpact> {
        const response = await api.get<FriendshipImpact>(`/api/friends/${friendshipId}/impact`);
        return response.data;
    }

    return {
        friends,
        receivedRequests,
        sentRequests,
        searchResults,
        isLoading,
        friendCount,
        pendingRequestCount,
        fetchFriends,
        fetchReceivedRequests,
        fetchSentRequests,
        sendFriendRequest,
        clearSearchResults,
        acceptRequest,
        rejectRequest,
        cancelRequest,
        deleteFriendship,
        searchUsers,
        getUserProfile,
        checkFriendshipImpact,
    };
});
