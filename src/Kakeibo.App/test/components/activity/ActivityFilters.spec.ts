import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import ActivityFilters from "@/components/activity/ActivityFilters.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountFilters() {
    return mount(ActivityFilters, {
        global: {
            plugins: [i18n],
        },
    });
}

describe("ActivityFilters", () => {
    it("renders start date input", () => {
        const wrapper = mountFilters();
        const inputs = wrapper.findAll('input[type="date"]');
        expect(inputs.length).toBeGreaterThanOrEqual(1);
    });

    it("renders end date input", () => {
        const wrapper = mountFilters();
        const inputs = wrapper.findAll('input[type="date"]');
        expect(inputs.length).toBeGreaterThanOrEqual(2);
    });

    it("renders action type select with all-actions option", () => {
        const wrapper = mountFilters();
        const select = wrapper.find("select");
        expect(select.exists()).toBe(true);
        // The "All actions" option should be present
        const options = wrapper.findAll("option");
        const allOption = options.find((o) => o.text() === "All actions");
        expect(allOption).toBeDefined();
    });

    it("renders Apply button", () => {
        const wrapper = mountFilters();
        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        expect(applyButton).toBeDefined();
    });

    it("emits apply event with empty filters when nothing is changed", async () => {
        const wrapper = mountFilters();
        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        await applyButton?.trigger("click");

        const emitted = wrapper.emitted("apply");
        expect(emitted).toBeTruthy();
        const payload = emitted?.[0]?.[0] as Record<string, unknown>;
        // Empty strings become undefined
        expect(payload.start).toBeUndefined();
        expect(payload.end).toBeUndefined();
        expect(payload.type).toBeUndefined();
        // Page resets to 1
        expect(payload.page).toBe(1);
    });

    it("emits apply event with start date when filled in", async () => {
        const wrapper = mountFilters();
        const startInput = wrapper.findAll('input[type="date"]')[0];
        await startInput.setValue("2026-01-01");

        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        await applyButton?.trigger("click");

        const payload = wrapper.emitted("apply")?.[0]?.[0] as Record<string, unknown>;
        expect(payload.start).toBe("2026-01-01");
    });

    it("emits apply event with end date when filled in", async () => {
        const wrapper = mountFilters();
        const endInput = wrapper.findAll('input[type="date"]')[1];
        await endInput.setValue("2026-03-31");

        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        await applyButton?.trigger("click");

        const payload = wrapper.emitted("apply")?.[0]?.[0] as Record<string, unknown>;
        expect(payload.end).toBe("2026-03-31");
    });

    it("emits apply event with action type when selected", async () => {
        const wrapper = mountFilters();
        const select = wrapper.find("select");
        await select.setValue("transaction.recorded");

        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        await applyButton?.trigger("click");

        const payload = wrapper.emitted("apply")?.[0]?.[0] as Record<string, unknown>;
        expect(payload.type).toBe("transaction.recorded");
    });

    it("resets page to 1 on every apply", async () => {
        const wrapper = mountFilters();
        const applyButton = wrapper.findAll("button").find((b) => b.text() === "Apply filters");
        await applyButton?.trigger("click");

        const payload = wrapper.emitted("apply")?.[0]?.[0] as Record<string, unknown>;
        expect(payload.page).toBe(1);
    });
});
