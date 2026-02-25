import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import { createPinia, setActivePinia } from "pinia";
import DeleteAccountSection from "@/components/settings/DeleteAccountSection.vue";

vi.mock("@/lib/axios");
vi.mock("vue-router", () => ({
    useRouter: () => ({ replace: vi.fn() }),
}));

const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
        en: {
            settings: {
                deleteAccount: {
                    title: "Delete Account",
                    description: "Your account will be permanently deleted.",
                    confirm: "Delete my account",
                    password: "Confirm your password",
                    passwordPlaceholder: "Enter your password",
                    warning: "This action cannot be undone.",
                    success: "Account deletion requested.",
                },
            },
            common: { cancel: "Cancel", errorGeneric: "Something went wrong." },
        },
    },
});

describe("DeleteAccountSection", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    it("renders danger section with title", () => {
        const wrapper = mount(DeleteAccountSection, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("Delete Account");
    });

    it("renders delete button to trigger dialog", () => {
        const wrapper = mount(DeleteAccountSection, { global: { plugins: [i18n] } });
        const deleteBtn = wrapper.find("button");
        expect(deleteBtn.exists()).toBe(true);
        expect(deleteBtn.text()).toBe("Delete my account");
    });

    it("shows warning message", () => {
        const wrapper = mount(DeleteAccountSection, { global: { plugins: [i18n] } });
        expect(wrapper.text()).toContain("This action cannot be undone.");
    });
});
