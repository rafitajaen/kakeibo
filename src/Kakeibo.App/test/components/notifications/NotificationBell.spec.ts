import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import NotificationBell from "@/components/notifications/NotificationBell.vue";
import { useNotificationsStore } from "@/stores/notifications";
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

const apiMock = api as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountBell() {
    return mount(NotificationBell, {
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                NotificationList: { template: "<div data-testid='notification-list-stub' />" },
            },
        },
    });
}

describe("NotificationBell", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders the bell button", () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        const wrapper = mountBell();
        expect(wrapper.find('[data-testid="notification-bell"]').exists()).toBe(true);
    });

    it("hides unread badge when there are no unread notifications", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        const wrapper = mountBell();
        const store = useNotificationsStore();
        store.notifications = [];

        await wrapper.vm.$nextTick();

        expect(wrapper.find('[data-testid="unread-badge"]').exists()).toBe(false);
    });

    it("shows unread badge with count when there are unread notifications", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        const wrapper = mountBell();
        const store = useNotificationsStore();
        store.notifications = [
            {
                id: "n-1",
                type: "budget.warning",
                title: "Budget warning",
                body: "You are nearing your limit",
                isRead: false,
                createdAt: "2026-03-01T10:00:00Z",
            },
            {
                id: "n-2",
                type: "goal.milestone",
                title: "Milestone reached",
                body: "25% complete",
                isRead: true,
                createdAt: "2026-03-01T09:00:00Z",
            },
        ];

        await wrapper.vm.$nextTick();

        const badge = wrapper.find('[data-testid="unread-badge"]');
        expect(badge.exists()).toBe(true);
        expect(badge.text()).toBe("1");
    });

    it("shows 9+ badge when unread count exceeds 9", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        const wrapper = mountBell();
        const store = useNotificationsStore();
        store.notifications = Array.from({ length: 10 }, (_, i) => ({
            id: `n-${i}`,
            type: "budget.warning",
            title: "Warning",
            body: "Body",
            isRead: false,
            createdAt: "2026-03-01T10:00:00Z",
        }));

        await wrapper.vm.$nextTick();

        expect(wrapper.find('[data-testid="unread-badge"]').text()).toBe("9+");
    });

    it("toggles the notification list panel on click", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        const wrapper = mountBell();

        expect(wrapper.find('[data-testid="notification-list-stub"]').exists()).toBe(false);

        await wrapper.find('[data-testid="notification-bell"]').trigger("click");
        expect(wrapper.find('[data-testid="notification-list-stub"]').exists()).toBe(true);

        await wrapper.find('[data-testid="notification-bell"]').trigger("click");
        expect(wrapper.find('[data-testid="notification-list-stub"]').exists()).toBe(false);
    });

    it("calls fetchNotifications on mount", async () => {
        apiMock.get.mockResolvedValueOnce({ data: { unreadCount: 0, items: [] } });
        mountBell();

        await new Promise((r) => setTimeout(r, 0));

        expect(apiMock.get).toHaveBeenCalledWith(
            "/api/notifications",
            expect.objectContaining({ params: expect.objectContaining({ page: 1 }) }),
        );
    });
});
