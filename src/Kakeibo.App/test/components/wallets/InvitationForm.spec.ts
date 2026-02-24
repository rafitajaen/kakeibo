import { describe, it, expect, vi, afterEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import InvitationForm from "@/components/wallets/InvitationForm.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountForm(props: Record<string, unknown> = {}) {
    return mount(InvitationForm, {
        props,
        global: { plugins: [i18n] },
        attachTo: document.body,
    });
}

describe("InvitationForm", () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders the email input field", () => {
        const wrapper = mountForm();
        expect(wrapper.find("input[type='email']").exists()).toBe(true);
    });

    it("renders the submit button with the correct label", () => {
        const wrapper = mountForm();
        expect(wrapper.find("button[type='submit']").text()).toBe("Send invitation");
    });

    it("disables the submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find("button[type='submit']");
        expect((button.element as HTMLButtonElement).disabled).toBe(true);
    });

    it("emits submit event with the email when form is valid", async () => {
        const wrapper = mountForm();
        await wrapper.vm.setFieldValue("email", "invite@example.com");
        await wrapper.vm.onSubmit(new Event("submit"));
        await flushPromises();

        const emitted = wrapper.emitted("submit") as Array<[string]>;
        expect(emitted).toBeDefined();
        expect(emitted[0][0]).toBe("invite@example.com");
    });

    it("does not emit submit when the email field is empty", async () => {
        const wrapper = mountForm();
        await wrapper.find("form").trigger("submit");
        await flushPromises();

        expect(wrapper.emitted("submit")).toBeUndefined();
    });

    it("does not emit submit when the email is malformed", async () => {
        const wrapper = mountForm();
        await wrapper.vm.setFieldValue("email", "not-an-email");
        await wrapper.vm.onSubmit(new Event("submit"));
        await flushPromises();

        expect(wrapper.emitted("submit")).toBeUndefined();
    });
});
