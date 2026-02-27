import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { flushPromises } from "@vue/test-utils";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import PreferencesForm from "@/components/notifications/PreferencesForm.vue";
import { useNotificationsStore } from "@/stores/notifications";
import type { NotificationPreferences } from "@/stores/notifications";
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

const defaultPrefs: NotificationPreferences = {
    emailBudgetAlerts: true,
    emailGoalMilestones: true,
    emailInvitations: true,
    emailRecurringUpdates: true,
    pushBudgetAlerts: true,
    pushGoalMilestones: true,
    pushInvitations: true,
    pushRecurringUpdates: true,
};

function mountForm() {
    return mount(PreferencesForm, {
        global: {
            plugins: [i18n, createPinia()],
        },
    });
}

describe("PreferencesForm", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders 8 checkboxes for all preference toggles", () => {
        const wrapper = mountForm();
        const checkboxes = wrapper.findAll('input[type="checkbox"]');
        expect(checkboxes).toHaveLength(8);
    });

    it("syncs checkboxes from store.preferences when set", async () => {
        const wrapper = mountForm();
        const store = useNotificationsStore();
        store.preferences = {
            ...defaultPrefs,
            emailBudgetAlerts: false,
            pushGoalMilestones: false,
        };

        await wrapper.vm.$nextTick();

        const checkboxes = wrapper.findAll('input[type="checkbox"]') as ReturnType<
            typeof wrapper.find
        >[];
        // emailBudgetAlerts is the first checkbox
        expect((checkboxes[0].element as HTMLInputElement).checked).toBe(false);
    });

    it("renders save button", () => {
        const wrapper = mountForm();
        const saveButton = wrapper.findAll("button").find((b) => b.text().includes("Save"));
        expect(saveButton).toBeDefined();
    });

    it("calls updatePreferences with current state when save is clicked", async () => {
        apiMock.put.mockResolvedValueOnce({});
        const wrapper = mountForm();
        const store = useNotificationsStore();
        store.preferences = { ...defaultPrefs };

        await wrapper.vm.$nextTick();

        const saveButton = wrapper.findAll("button").find((b) => b.text().includes("Save"));
        await saveButton?.trigger("click");

        expect(apiMock.put).toHaveBeenCalledWith(
            "/api/notifications/preferences",
            expect.objectContaining({
                emailBudgetAlerts: true,
                emailGoalMilestones: true,
            }),
        );
    });

    it("shows saved confirmation after successful save", async () => {
        apiMock.put.mockResolvedValueOnce({});
        const wrapper = mountForm();

        expect(wrapper.text()).not.toContain("Preferences saved.");

        const saveButton = wrapper.findAll("button").find((b) => b.text().includes("Save"));
        await saveButton?.trigger("click");
        await flushPromises();

        expect(wrapper.text()).toContain("Preferences saved.");
    });

    it("toggles checkbox value in local state", async () => {
        const wrapper = mountForm();
        const store = useNotificationsStore();
        store.preferences = { ...defaultPrefs };

        await wrapper.vm.$nextTick();

        const firstCheckbox = wrapper.find('input[type="checkbox"]');
        expect((firstCheckbox.element as HTMLInputElement).checked).toBe(true);

        await firstCheckbox.trigger("click");
        await wrapper.vm.$nextTick();

        expect((firstCheckbox.element as HTMLInputElement).checked).toBe(false);
    });
});
