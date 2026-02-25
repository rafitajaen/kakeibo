import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import ProgressIndicator from "@/components/onboarding/ProgressIndicator.vue";

describe("ProgressIndicator", () => {
    it("renders the correct number of dots", () => {
        const wrapper = mount(ProgressIndicator, {
            props: { currentStep: 0, totalSteps: 4 },
        });
        const dots = wrapper.findAll("[class*='rounded-full']");
        expect(dots).toHaveLength(4);
    });

    it("highlights the active step dot with wider width", () => {
        const wrapper = mount(ProgressIndicator, {
            props: { currentStep: 1, totalSteps: 4 },
        });
        const dots = wrapper.findAll("[class*='rounded-full']");
        // Second dot (index 1) should have 'bg-primary'
        expect(dots[1].classes()).toContain("bg-primary");
    });

    it("inactive dots have muted class", () => {
        const wrapper = mount(ProgressIndicator, {
            props: { currentStep: 0, totalSteps: 4 },
        });
        const dots = wrapper.findAll("[class*='rounded-full']");
        expect(dots[1].classes()).toContain("bg-muted");
        expect(dots[2].classes()).toContain("bg-muted");
        expect(dots[3].classes()).toContain("bg-muted");
    });

    it("sets correct aria attributes", () => {
        const wrapper = mount(ProgressIndicator, {
            props: { currentStep: 2, totalSteps: 4 },
        });
        const container = wrapper.find("[role='progressbar']");
        expect(container.attributes("aria-valuenow")).toBe("3");
        expect(container.attributes("aria-valuemax")).toBe("4");
    });
});
