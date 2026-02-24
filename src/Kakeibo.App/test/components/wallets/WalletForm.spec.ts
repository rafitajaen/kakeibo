import { describe, it, expect, vi, afterEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import WalletForm from "@/components/wallets/WalletForm.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountForm(props: Record<string, unknown> = {}) {
    return mount(WalletForm, {
        props,
        global: { plugins: [i18n] },
        attachTo: document.body,
    });
}

describe("WalletForm", () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders the name field and submit button", () => {
        const wrapper = mountForm();
        expect(wrapper.find("input").exists()).toBe(true);
        expect(wrapper.find("button[type='submit']").exists()).toBe(true);
    });

    it("shows 'Create wallet' label in create mode", () => {
        const wrapper = mountForm();
        expect(wrapper.find("button[type='submit']").text()).toBe("Create wallet");
    });

    it("shows 'Save changes' label in edit mode when initialName is provided", () => {
        const wrapper = mountForm({ initialName: "Savings" });
        expect(wrapper.find("button[type='submit']").text()).toBe("Save changes");
    });

    it("pre-fills the name field when initialName is provided", () => {
        const wrapper = mountForm({ initialName: "Savings" });
        const input = wrapper.find("input");
        expect((input.element as HTMLInputElement).value).toBe("Savings");
    });

    it("emits submit event with name and type Personal when form is valid", async () => {
        // Use initialName so VeeValidate's initialValues contain a valid name.
        // Call onSubmit directly via defineExpose to bypass jsdom form submit
        // propagation issues between the VeeValidate / shadcn-vue Input stack.
        const wrapper = mountForm({ initialName: "New Wallet" });
        await wrapper.vm.onSubmit(new Event("submit"));
        await flushPromises();

        const emitted = wrapper.emitted("submit") as Array<[{ name: string; type: string }]>;
        expect(emitted).toBeDefined();
        expect(emitted[0][0].name).toBe("New Wallet");
        expect(emitted[0][0].type).toBe("Personal");
    });

    it("does not emit submit when name is empty", async () => {
        const wrapper = mountForm();
        await wrapper.find("form").trigger("submit");
        await flushPromises();

        expect(wrapper.emitted("submit")).toBeUndefined();
    });

    it("disables the submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find("button[type='submit']");
        expect((button.element as HTMLButtonElement).disabled).toBe(true);
    });
});
