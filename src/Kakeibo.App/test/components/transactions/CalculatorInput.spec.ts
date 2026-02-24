import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import CalculatorInput from "@/components/transactions/CalculatorInput.vue";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

function mountInput(modelValue: number) {
    return mount(CalculatorInput, {
        props: { modelValue },
        global: { plugins: [i18n, createPinia()] },
    });
}

describe("CalculatorInput", () => {
    it("renders the current modelValue as the initial display text", () => {
        const wrapper = mountInput(150);
        const input = wrapper.find("input");
        expect(input.element.value).toBe("150");
    });

    it("evaluates '100 + 50' to 150 on blur", async () => {
        const wrapper = mountInput(0);
        const input = wrapper.find("input");
        await input.setValue("100 + 50");
        await input.trigger("blur");
        expect(wrapper.emitted("update:modelValue")?.[0]).toEqual([150]);
    });

    it("evaluates '200 / 4' to 50 on blur", async () => {
        const wrapper = mountInput(0);
        const input = wrapper.find("input");
        await input.setValue("200 / 4");
        await input.trigger("blur");
        expect(wrapper.emitted("update:modelValue")?.[0]).toEqual([50]);
    });

    it("evaluates '3 * 33.33' and rounds to 2 decimals", async () => {
        const wrapper = mountInput(0);
        const input = wrapper.find("input");
        await input.setValue("3 * 33.33");
        await input.trigger("blur");
        const emitted = wrapper.emitted("update:modelValue")?.[0]?.[0] as number;
        expect(emitted).toBeCloseTo(99.99, 2);
    });

    it("does not emit on invalid expression, reverts display to modelValue", async () => {
        const wrapper = mountInput(42);
        const input = wrapper.find("input");
        await input.setValue("abc + 10");
        await input.trigger("blur");
        // No emission for invalid input
        expect(wrapper.emitted("update:modelValue")).toBeUndefined();
        // Display reverts to the previous valid modelValue
        expect(input.element.value).toBe("42");
    });

    it("evaluates expression on Enter key", async () => {
        const wrapper = mountInput(0);
        const input = wrapper.find("input");
        await input.setValue("50 + 50");
        await input.trigger("keydown", { key: "Enter" });
        expect(wrapper.emitted("update:modelValue")?.[0]).toEqual([100]);
    });

    it("does not emit for zero or negative results", async () => {
        const wrapper = mountInput(10);
        const input = wrapper.find("input");
        // 5 - 10 = -5 (invalid — amount must be positive)
        await input.setValue("5 - 10");
        await input.trigger("blur");
        expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    });
});
