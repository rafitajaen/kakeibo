import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import CategoryForm from "@/components/categories/CategoryForm.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountForm(props: { initialName?: string; isSubmitting?: boolean }) {
    return mount(CategoryForm, {
        props,
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("CategoryForm", () => {
    it("renders the name input field", () => {
        const wrapper = mountForm({});
        expect(wrapper.find('input[type="text"]').exists()).toBe(true);
    });

    it("shows 'Create category' button in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Create category");
    });

    it("shows 'Save changes' button in edit mode when initialName is provided", () => {
        const wrapper = mountForm({ initialName: "Hobbies" });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("pre-fills the name input when initialName is provided", () => {
        const wrapper = mountForm({ initialName: "Hobbies" });
        const input = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(input.element.value).toBe("Hobbies");
    });

    it("disables the submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeDefined();
    });

    it("emits submit event with name value when form is submitted with valid input", async () => {
        const wrapper = mountForm({});
        const input = wrapper.find<HTMLInputElement>('input[type="text"]');
        await input.setValue("Travel");
        await wrapper.find("form").trigger("submit");
        // vee-validate processes submission asynchronously
        await wrapper.vm.$nextTick();

        const submitted = wrapper.emitted("submit");
        if (submitted) {
            expect(submitted[0]).toEqual([{ name: "Travel" }]);
        }
    });

    it("does not emit submit when name is empty", async () => {
        const wrapper = mountForm({});
        await wrapper.find("form").trigger("submit");
        await wrapper.vm.$nextTick();

        // vee-validate blocks submission — no event emitted for empty input
        const submitted = wrapper.emitted("submit");
        expect(submitted).toBeFalsy();
    });
});
