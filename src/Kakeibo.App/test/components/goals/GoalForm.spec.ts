import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import GoalForm from "@/components/goals/GoalForm.vue";
import type { Goal } from "@/stores/goals";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const mockWallets: Wallet[] = [
    {
        id: "wallet-1",
        name: "Checking",
        type: "Personal",
        currency: "EUR",
        balance: 1000,
        isArchived: false,
        createdAt: "2026-01-01T00:00:00Z",
    },
];

const mockGoal: Goal = {
    id: "goal-1",
    name: "Europe Vacation",
    targetAmount: 5000,
    deadline: "2026-12-31",
    walletId: "wallet-1",
    walletName: "Checking",
    currentProgress: 1000,
    lastMilestone: 0,
    createdAt: "2026-01-01T00:00:00Z",
};

function mountForm(props: { goal?: Goal; isSubmitting?: boolean }) {
    return mount(GoalForm, {
        props: {
            goal: props.goal,
            wallets: mockWallets,
            isSubmitting: props.isSubmitting,
        },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub reka-ui Select components to avoid portal/jsdom issues
                Select: {
                    template: '<div class="select-stub"><slot /></div>',
                    props: ["modelValue"],
                    emits: ["update:modelValue"],
                },
                SelectTrigger: { template: "<div />" },
                SelectContent: { template: "<div><slot /></div>" },
                SelectItem: { template: "<div />", props: ["value"] },
                SelectValue: { template: "<div />" },
            },
        },
    });
}

describe("GoalForm", () => {
    it("renders name, targetAmount, and deadline input fields", () => {
        const wrapper = mountForm({});
        const inputs = wrapper.findAll("input");
        // name (text), targetAmount (number), deadline (date) = at least 3
        expect(inputs.length).toBeGreaterThanOrEqual(3);
    });

    it("shows 'Create goal' submit button in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Create goal");
    });

    it("shows 'Save changes' submit button in edit mode", () => {
        const wrapper = mountForm({ goal: mockGoal });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("pre-fills the name input in edit mode", () => {
        const wrapper = mountForm({ goal: mockGoal });
        const nameInput = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(nameInput.element.value).toBe("Europe Vacation");
    });

    it("pre-fills the targetAmount input in edit mode", () => {
        const wrapper = mountForm({ goal: mockGoal });
        const amountInput = wrapper.find<HTMLInputElement>('input[type="number"]');
        expect(Number(amountInput.element.value)).toBe(5000);
    });

    it("disables the submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeDefined();
    });

    it("submit button is enabled by default", () => {
        const wrapper = mountForm({});
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeUndefined();
    });
});
