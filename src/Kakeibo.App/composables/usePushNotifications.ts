import { useNotificationsStore } from "@/stores/notifications";

// Converts a base64url VAPID public key string to a Uint8Array for pushManager.subscribe.
function urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
    const rawData = atob(base64);
    return Uint8Array.from(rawData, (c) => c.charCodeAt(0));
}

export function usePushNotifications() {
    // Requests notification permission and subscribes the browser to Web Push.
    // No-ops silently if the browser doesn't support Push API or the user denies permission.
    async function initPushSubscription(): Promise<void> {
        if (
            !("Notification" in window) ||
            !("serviceWorker" in navigator) ||
            !("PushManager" in window)
        ) {
            return;
        }

        const vapidKey = import.meta.env.VITE_VAPID_PUBLIC_KEY as string | undefined;
        if (!vapidKey) {
            return;
        }

        let permission = Notification.permission;
        if (permission === "default") {
            permission = await Notification.requestPermission();
        }

        if (permission !== "granted") {
            return;
        }

        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidKey) as unknown as BufferSource,
            });

            const store = useNotificationsStore();
            await store.registerPushSubscription(subscription);
        } catch {
            // Subscription failed (e.g. blocked by browser policy) — fail silently
        }
    }

    return { initPushSubscription };
}
