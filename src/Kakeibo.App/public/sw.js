// Service Worker — handles precaching (via Workbox) and Web Push notifications for Kakeibo.
// self.__WB_MANIFEST is injected by vite-plugin-pwa at build time.
// During development this is an empty array so the SW still loads without errors.
const precacheManifest = self.__WB_MANIFEST || [];

// Minimal precaching: on install, cache all assets in the injected manifest
self.addEventListener("install", (event) => {
    event.waitUntil(
        caches
            .open("kakeibo-precache-v1")
            .then((cache) =>
                cache.addAll(
                    precacheManifest.map((entry) =>
                        typeof entry === "string" ? entry : entry.url,
                    ),
                ),
            ),
    );
    self.skipWaiting();
});

// Activate — clean up old caches and claim clients
self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches
            .keys()
            .then((keys) =>
                Promise.all(
                    keys.filter((k) => k !== "kakeibo-precache-v1").map((k) => caches.delete(k)),
                ),
            )
            .then(() => self.clients.claim()),
    );
});

// Fetch — serve cached assets when offline
self.addEventListener("fetch", (event) => {
    // Only handle same-origin navigation requests
    if (event.request.mode === "navigate") {
        event.respondWith(
            fetch(event.request).catch(() =>
                caches.match("/index.html").then((r) => r || Response.error()),
            ),
        );
    }
});

// Web Push — show a notification when a push message arrives
self.addEventListener("push", (event) => {
    const data = event.data?.json() ?? {};
    const title = data.title ?? "Kakeibo";
    const options = {
        body: data.body ?? "",
        icon: "/icon-192.png",
    };
    event.waitUntil(self.registration.showNotification(title, options));
});

// Notification click — open the app when the user taps the notification
self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil(clients.openWindow("/"));
});
