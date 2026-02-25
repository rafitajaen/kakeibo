import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { createPinia, setActivePinia } from "pinia";
import SessionsList from "@/components/settings/SessionsList.vue";
import { useSettingsStore } from "@/stores/settings";

vi.mock("@/lib/axios");

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            settings: {
                sessions: {
                    description: "Manage devices.",
                    revoke: "Revoke",
                    empty: "No active sessions found.",
                    unknownDevice: "Unknown device",
                },
            },
        },
    },
});

describe("SessionsList", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    it("shows empty state when no sessions", () => {
        const wrapper = mount(SessionsList, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("No active sessions found.");
    });

    it("renders sessions list when sessions exist", () => {
        const settingsStore = useSettingsStore();
        settingsStore.sessions = [
            { id: "1", ipAddress: "192.168.1.1", userAgent: "Chrome/120", createdAt: "2026-03-01" },
        ];
        const wrapper = mount(SessionsList, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Chrome/120");
        expect(wrapper.text()).toContain("Revoke");
    });

    it("shows revoke button for each session", () => {
        const settingsStore = useSettingsStore();
        settingsStore.sessions = [
            { id: "1", ipAddress: "1.2.3.4", userAgent: "Firefox", createdAt: "2026-03-01" },
            { id: "2", ipAddress: "5.6.7.8", userAgent: "Safari", createdAt: "2026-03-02" },
        ];
        const wrapper = mount(SessionsList, { global: { plugins: [i18n] } });
        const revokeButtons = wrapper.findAll("button");
        expect(revokeButtons).toHaveLength(2);
    });
});
