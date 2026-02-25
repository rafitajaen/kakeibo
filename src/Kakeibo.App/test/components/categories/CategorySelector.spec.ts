import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import CategorySelector from "@/components/categories/CategorySelector.vue";
import type { Category } from "@/stores/categories";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const categories: Category[] = [
    { id: "cat-1", name: "Housing", isSystem: true, isArchived: false },
    { id: "cat-2", name: "Food & Dining", isSystem: true, isArchived: false },
    { id: "custom-1", name: "Hobbies", isSystem: false, isArchived: false },
];

function mountSelector(props: {
    categories: Category[];
    modelValue?: string;
    placeholder?: string;
}) {
    return mount(CategorySelector, {
        props,
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("CategorySelector", () => {
    it("renders a select trigger element", () => {
        const wrapper = mountSelector({ categories });
        expect(wrapper.find("button").exists()).toBe(true);
    });

    it("uses default placeholder from i18n when no placeholder prop is provided", () => {
        const wrapper = mountSelector({ categories });
        // Default placeholder comes from t('categories.form.name') = "Name"
        expect(wrapper.text()).toContain("Name");
    });

    it("uses custom placeholder when provided", () => {
        const wrapper = mountSelector({ categories, placeholder: "Pick a category" });
        expect(wrapper.text()).toContain("Pick a category");
    });

    it("renders without errors when modelValue is provided", () => {
        const wrapper = mountSelector({ categories, modelValue: "cat-1" });
        expect(wrapper.exists()).toBe(true);
    });

    it("renders without errors when categories list is empty", () => {
        const wrapper = mountSelector({ categories: [] });
        expect(wrapper.exists()).toBe(true);
    });
});
