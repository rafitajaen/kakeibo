import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import NotificationList from "@/components/notifications/NotificationList.vue";
import { useNotificationsStore } from "@/stores/notifications";
import type { AppNotification } from "@/stores/notifications";
import en from "@/locales/en.json";

vi.mock("@/lib/axios", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    },
}));

import api from "@/lib/axios";

const apiMock = api as unknown as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({ history: createMemoryHistory(), routes: [] });

const makeNotification = (overrides: Partial<AppNotification> = {}): AppNotification => ({
    id: "n-1",
    type: "budget.warning",
    title: "Budget warning",
    body: "You are nearing your limit",
    isRead: false,
    createdAt: "2026-03-01T10:00:00Z",
    ...overrides,
});

function mountList() {
    return mount(NotificationList, {
        global: {
            plugins: [i18n, createPinia(), router],
        },
    });
}

describe("NotificationList", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("shows empty state message when there are no notifications", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [];

        await wrapper.vm.$nextTick();

        expect(wrapper.text()).toContain("No notifications");
    });

    it("renders one item per notification", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [
            makeNotification({ id: "n-1" }),
            makeNotification({ id: "n-2", title: "Second" }),
        ];

        await wrapper.vm.$nextTick();

        expect(wrapper.findAll('[data-testid="notification-item"]')).toHaveLength(2);
    });

    it("renders notification title and body", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [
            makeNotification({ title: "Budget Alert", body: "You exceeded your limit" }),
        ];

        await wrapper.vm.$nextTick();

        expect(wrapper.text()).toContain("Budget Alert");
        expect(wrapper.text()).toContain("You exceeded your limit");
    });

    it("shows mark-all-read button when there are unread notifications", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [makeNotification({ isRead: false })];

        await wrapper.vm.$nextTick();

        expect(wrapper.text()).toContain("Mark all as read");
    });

    it("hides mark-all-read button when all notifications are read", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [makeNotification({ isRead: true })];

        await wrapper.vm.$nextTick();

        expect(wrapper.text()).not.toContain("Mark all as read");
    });

    it("calls markAsRead when clicking on a notification item", async () => {
        apiMock.put.mockResolvedValueOnce({});
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [makeNotification({ id: "n-1", isRead: false })];

        await wrapper.vm.$nextTick();

        await wrapper.find('[data-testid="notification-item"]').trigger("click");

        expect(apiMock.put).toHaveBeenCalledWith("/api/notifications/n-1/read");
    });

    it("calls markAllAsRead when clicking mark-all-read button", async () => {
        apiMock.put.mockResolvedValue({});
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [
            makeNotification({ id: "n-1", isRead: false }),
            makeNotification({ id: "n-2", isRead: false }),
        ];

        await wrapper.vm.$nextTick();

        const button = wrapper.findAll("button").find((b) => b.text().includes("Mark all as read"));
        await button?.trigger("click");

        expect(apiMock.put).toHaveBeenCalledTimes(2);
    });

    it("emits close event when close button is clicked", async () => {
        const wrapper = mountList();
        const store = useNotificationsStore();
        store.notifications = [];

        await wrapper.vm.$nextTick();

        const closeButton = wrapper.findAll("button").find((b) => b.text() === "✕");
        await closeButton?.trigger("click");

        expect(wrapper.emitted("close")).toBeTruthy();
    });
});
