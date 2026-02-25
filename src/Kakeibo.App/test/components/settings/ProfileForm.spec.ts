import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { createPinia, setActivePinia } from "pinia";
import ProfileForm from "@/components/settings/ProfileForm.vue";
import { useAuthStore } from "@/stores/auth";

vi.mock("@/lib/axios");

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            settings: {
                profile: {
                    name: "Display name",
                    namePlaceholder: "Your name",
                    currency: "Currency",
                    currencyPlaceholder: "Select currency",
                    save: "Save changes",
                    saved: "Saved!",
                },
            },
        },
    },
});

describe("ProfileForm", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        const auth = useAuthStore();
        auth.user = {
            id: "1",
            email: "test@example.com",
            role: "User",
            currency: "USD",
            isVerified: true,
            name: "Test User",
        };
    });

    it("renders name and currency fields", () => {
        const wrapper = mount(ProfileForm, { global: { plugins: [i18n] } });
        expect(wrapper.find("input").exists()).toBe(true);
        expect(wrapper.text()).toContain("Display name");
        expect(wrapper.text()).toContain("Currency");
    });

    it("shows Save button", () => {
        const wrapper = mount(ProfileForm, { global: { plugins: [i18n] } });
        const btn = wrapper.find("button[type='submit']");
        expect(btn.exists()).toBe(true);
        expect(btn.text()).toBe("Save changes");
    });

    it("renders without errors when user is null", () => {
        const auth = useAuthStore();
        auth.user = null;
        const wrapper = mount(ProfileForm, { global: { plugins: [i18n] } });
        expect(wrapper.find("input").exists()).toBe(true);
    });
});
