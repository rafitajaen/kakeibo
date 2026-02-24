import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import CategoryList from "@/components/categories/CategoryList.vue";
import type { Category } from "@/stores/categories";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const systemCategory: Category = {
    id: "10000000-0000-0000-0000-000000000001",
    name: "Housing",
    isSystem: true,
    isArchived: false,
};

const customCategory: Category = {
    id: "custom-1",
    name: "Hobbies",
    isSystem: false,
    isArchived: false,
};

const archivedCategory: Category = {
    id: "custom-2",
    name: "Old Category",
    isSystem: false,
    isArchived: true,
};

function mountList(categories: Category[], showActions = false) {
    return mount(CategoryList, {
        props: { categories, showActions },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("CategoryList", () => {
    it("shows empty message when there are no categories", () => {
        const wrapper = mountList([]);
        expect(wrapper.text()).toContain("No custom categories yet");
    });

    it("renders category names", () => {
        const wrapper = mountList([systemCategory, customCategory]);
        expect(wrapper.text()).toContain("Housing");
        expect(wrapper.text()).toContain("Hobbies");
    });

    it("does not show edit/archive buttons when showActions is false", () => {
        const wrapper = mountList([customCategory], false);
        expect(wrapper.text()).not.toContain("Edit");
        expect(wrapper.text()).not.toContain("Archive");
    });

    it("shows edit and archive buttons for non-system non-archived category when showActions is true", () => {
        const wrapper = mountList([customCategory], true);
        expect(wrapper.text()).toContain("Edit");
        expect(wrapper.text()).toContain("Archive");
    });

    it("does not show action buttons for system categories even when showActions is true", () => {
        const wrapper = mountList([systemCategory], true);
        expect(wrapper.text()).not.toContain("Edit");
        expect(wrapper.text()).not.toContain("Archive");
    });

    it("does not show action buttons for archived categories even when showActions is true", () => {
        const wrapper = mountList([archivedCategory], true);
        // Check that no button elements have "Edit" or "Archive" text
        // (the Badge renders "Archived" which contains "Archive" as a substring)
        const buttons = wrapper.findAll("button");
        expect(buttons.every((b) => b.text() !== "Edit")).toBe(true);
        expect(buttons.every((b) => b.text() !== "Archive")).toBe(true);
    });

    it("emits edit event when edit button is clicked", async () => {
        const wrapper = mountList([customCategory], true);
        const buttons = wrapper.findAll("button");
        const editButton = buttons.find((b) => b.text() === "Edit");
        await editButton?.trigger("click");
        expect(wrapper.emitted("edit")).toBeTruthy();
        expect(wrapper.emitted("edit")?.[0]?.[0]).toEqual(customCategory);
    });

    it("emits archive event when archive button is clicked", async () => {
        const wrapper = mountList([customCategory], true);
        const buttons = wrapper.findAll("button");
        const archiveButton = buttons.find((b) => b.text() === "Archive");
        await archiveButton?.trigger("click");
        expect(wrapper.emitted("archive")).toBeTruthy();
        expect(wrapper.emitted("archive")?.[0]?.[0]).toEqual(customCategory);
    });
});
