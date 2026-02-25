import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import GoalList from "@/components/goals/GoalList.vue";
import type { Goal } from "@/stores/goals";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeGoal = (overrides: Partial<Goal> = {}): Goal => ({
    id: "goal-1",
    name: "Europe Vacation",
    targetAmount: 5000,
    deadline: "2026-12-31",
    walletId: "wallet-1",
    walletName: "Checking",
    currentProgress: 1000,
    lastMilestone: 0,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountList(goals: Goal[]) {
    return mount(GoalList, {
        props: { goals },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub reka-ui AlertDialog components to avoid portal/teleport issues in jsdom
                AlertDialog: { template: "<div><slot /></div>" },
                AlertDialogTrigger: { template: "<div><slot /></div>" },
                AlertDialogContent: { template: "<div><slot /></div>" },
                AlertDialogHeader: { template: "<div><slot /></div>" },
                AlertDialogTitle: { template: "<div><slot /></div>" },
                AlertDialogDescription: { template: "<div><slot /></div>" },
                AlertDialogFooter: { template: "<div><slot /></div>" },
                AlertDialogAction: {
                    template: "<button @click=\"$emit('click')\"><slot /></button>",
                    emits: ["click"],
                },
                AlertDialogCancel: { template: "<button><slot /></button>" },
            },
        },
    });
}

describe("GoalList", () => {
    it("renders goal name and wallet name", () => {
        const wrapper = mountList([makeGoal()]);
        expect(wrapper.text()).toContain("Europe Vacation");
        expect(wrapper.text()).toContain("Checking");
    });

    it("renders progress amounts", () => {
        const wrapper = mountList([makeGoal({ currentProgress: 1000, targetAmount: 5000 })]);
        expect(wrapper.text()).toContain("1000.00");
        expect(wrapper.text()).toContain("5000.00");
    });

    it("renders formatted deadline when present", () => {
        const wrapper = mountList([makeGoal({ deadline: "2026-12-31" })]);
        // date-fns 'PP' format: Dec 31, 2026
        expect(wrapper.text()).toContain("Dec 31, 2026");
    });

    it("hides deadline section when deadline is null", () => {
        const wrapper = mountList([makeGoal({ deadline: null })]);
        expect(wrapper.text()).not.toContain("Deadline");
    });

    it("renders the progress bar element", () => {
        const wrapper = mountList([makeGoal()]);
        expect(wrapper.find('[role="progressbar"]').exists()).toBe(true);
    });

    it("emits delete event when confirm button is clicked", async () => {
        const wrapper = mountList([makeGoal()]);
        // The AlertDialogAction stub renders with data-testid="delete-confirm" inherited via attrs
        const confirmButton = wrapper.find('[data-testid="delete-confirm"]');
        await confirmButton.trigger("click");
        expect(wrapper.emitted("delete")).toBeTruthy();
        expect(wrapper.emitted("delete")?.[0]?.[0]).toBe("goal-1");
    });

    it("renders one item per goal with data-testid", () => {
        const goals = [makeGoal(), makeGoal({ id: "goal-2", name: "Second Goal" })];
        const wrapper = mountList(goals);
        expect(wrapper.findAll('[data-testid="goal-item"]')).toHaveLength(2);
    });

    it("renders edit button for each goal", () => {
        const wrapper = mountList([makeGoal()]);
        const editButton = wrapper.findAll("button").find((b) => b.text() === "Edit");
        expect(editButton).toBeDefined();
    });
});
