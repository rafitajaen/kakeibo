import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { createPinia, setActivePinia } from "pinia";
import PasswordChangeForm from "@/components/settings/PasswordChangeForm.vue";

vi.mock("@/lib/axios");

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            settings: {
                security: {
                    currentPassword: "Current password",
                    newPassword: "New password",
                    confirmPassword: "Confirm new password",
                    save: "Change password",
                    saved: "Password changed!",
                },
            },
            common: { errorGeneric: "Something went wrong." },
        },
    },
});

describe("PasswordChangeForm", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    it("renders three password fields", () => {
        const wrapper = mount(PasswordChangeForm, { global: { plugins: [i18n] } });
        const inputs = wrapper.findAll("input[type='password']");
        expect(inputs).toHaveLength(3);
    });

    it("renders change password button", () => {
        const wrapper = mount(PasswordChangeForm, { global: { plugins: [i18n] } });
        const btn = wrapper.find("button[type='submit']");
        expect(btn.exists()).toBe(true);
        expect(btn.text()).toBe("Change password");
    });

    it("labels are present", () => {
        const wrapper = mount(PasswordChangeForm, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Current password");
        expect(wrapper.text()).toContain("New password");
        expect(wrapper.text()).toContain("Confirm new password");
    });
});
