import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import GoalSummary from "@/components/dashboard/GoalSummary.vue";
import type { Goal } from "@/stores/goals";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        {
            path: "/goals/:id/edit",
            name: "goal-edit",
            component: { template: "<div/>" },
        },
        {
            path: "/goals/new",
            name: "goal-create",
            component: { template: "<div/>" },
        },
    ],
});

const makeGoal = (overrides: Partial<Goal> = {}): Goal => ({
    id: "g-1",
    name: "Europe Vacation",
    targetAmount: 5000,
    deadline: null,
    walletId: "w-1",
    walletName: "Savings",
    currentProgress: 2500,
    lastMilestone: 50,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountComponent(goals: Goal[], isLoading = false) {
    return mount(GoalSummary, {
        props: { goals, isLoading },
        global: { plugins: [i18n, createPinia(), router] },
    });
}

describe("GoalSummary", () => {
    it("shows skeleton placeholders when isLoading is true", () => {
        const wrapper = mountComponent([], true);
        expect(wrapper.findAll('[data-slot="skeleton"]').length).toBeGreaterThan(0);
    });

    it("shows empty state with create link when no goals", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("No active goals");
        expect(wrapper.text()).toContain("Create goal");
    });

    it("renders goal name and percentage", () => {
        const wrapper = mountComponent([makeGoal()]);
        expect(wrapper.text()).toContain("Europe Vacation");
        expect(wrapper.text()).toContain("50%");
    });

    it("renders progress bar", () => {
        const wrapper = mountComponent([makeGoal()]);
        expect(wrapper.find('[role="progressbar"]').exists()).toBe(true);
    });

    it("renders goal summary title", () => {
        const wrapper = mountComponent([]);
        expect(wrapper.text()).toContain("Goal Summary");
    });
});
