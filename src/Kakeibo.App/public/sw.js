// Service Worker — handles Web Push notifications for Kakeibo.
self.addEventListener("push", (event) => {
    const data = event.data?.json() ?? {};
    const title = data.title ?? "Kakeibo";
    const options = {
        body: data.body ?? "",
        icon: "/vite.svg",
    };
    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil(clients.openWindow("/"));
});
