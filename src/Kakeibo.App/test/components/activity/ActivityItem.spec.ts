import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import ActivityItem from "@/components/activity/ActivityItem.vue";
import type { ActivityEntry } from "@/stores/activity";
import en from "@/locales/en.json";

// Standard i18n using the real locale file (nested keys resolve correctly).
const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

// Custom i18n with extra nested keys to verify the translation
// resolution logic in actionLabel() for ad-hoc action types.
const i18nNested = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            activity: {
                actions: {
                    custom: { event: "My Custom Event" },
                },
            },
        },
    },
});

function makeEntry(overrides: Partial<ActivityEntry> = {}): ActivityEntry {
    return {
        id: "ae-1",
        action: "transaction.recorded",
        entityType: "Transaction",
        entityId: "tx-abc",
        occurredAt: "2026-03-15T10:30:00Z",
        ...overrides,
    };
}

function mountItem(entry: ActivityEntry, customI18n = i18n) {
    return mount(ActivityItem, {
        props: { entry },
        global: { plugins: [customI18n] },
    });
}

describe("ActivityItem", () => {
    it("renders the activity item container", () => {
        const wrapper = mountItem(makeEntry());
        expect(wrapper.find('[data-testid="activity-item"]').exists()).toBe(true);
    });

    it("renders the action dot badge", () => {
        const wrapper = mountItem(makeEntry());
        // The dot is a div with bg-primary and rounded-full classes
        const dot = wrapper.find(".bg-primary.rounded-full");
        expect(dot.exists()).toBe(true);
    });

    it("displays translated label when i18n key resolves successfully", () => {
        // Uses nested i18n messages to verify the translation path works when keys
        // are accessed without literal dots (e.g., action "custom.event" resolves
        // to activity.actions.custom.event = "My Custom Event")
        const wrapper = mountItem(
            makeEntry({ action: "custom.event" }),
            i18nNested as unknown as typeof i18n,
        );
        expect(wrapper.text()).toContain("My Custom Event");
    });

    it("falls back to raw action string when translation key is not found", () => {
        const wrapper = mountItem(makeEntry({ action: "unknown.custom.action" }));
        // Should render the raw action key, not the i18n key path
        expect(wrapper.text()).toContain("unknown.custom.action");
    });

    it("translates known action types using nested locale keys", () => {
        // activity.actions.transaction.recorded is a nested key that resolves correctly
        const wrapper = mountItem(makeEntry({ action: "transaction.recorded" }));
        expect(wrapper.text()).toContain("Recorded transaction");
    });

    it("displays a relative timestamp using formatDistanceToNow", () => {
        // Use a date in the past so "ago" suffix is present
        const wrapper = mountItem(makeEntry({ occurredAt: "2020-01-01T00:00:00Z" }));
        const text = wrapper.text();
        // formatDistanceToNow with addSuffix: true adds "ago"
        expect(text).toContain("ago");
    });

    it("falls back to raw iso string when date parsing fails", () => {
        const wrapper = mountItem(makeEntry({ occurredAt: "not-a-date" }));
        expect(wrapper.text()).toContain("not-a-date");
    });
});
