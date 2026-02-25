import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import ActivityFeed from "@/components/activity/ActivityFeed.vue";
import type { ActivityEntry, ActivityFilters } from "@/stores/activity";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeEntry = (overrides: Partial<ActivityEntry> = {}): ActivityEntry => ({
    id: "ae-1",
    action: "transaction.recorded",
    entityType: "Transaction",
    entityId: "tx-1",
    occurredAt: "2026-03-01T10:00:00Z",
    ...overrides,
});

const defaultFilters: ActivityFilters = {
    start: undefined,
    end: undefined,
    type: undefined,
    page: 1,
    pageSize: 20,
};

function mountFeed(props: {
    activities: ActivityEntry[];
    total: number;
    filters?: Partial<ActivityFilters>;
    isLoading?: boolean;
}) {
    return mount(ActivityFeed, {
        props: {
            activities: props.activities,
            total: props.total,
            filters: { ...defaultFilters, ...props.filters },
            isLoading: props.isLoading ?? false,
        },
        global: {
            plugins: [i18n],
            stubs: {
                ActivityItem: {
                    template: "<div data-testid='activity-item'>{{ entry.action }}</div>",
                    props: ["entry"],
                },
            },
        },
    });
}

describe("ActivityFeed", () => {
    it("shows loading skeleton when isLoading is true", () => {
        const wrapper = mountFeed({ activities: [], total: 0, isLoading: true });
        expect(wrapper.find(".animate-pulse").exists()).toBe(true);
        expect(wrapper.find('[data-testid="activity-empty"]').exists()).toBe(false);
    });

    it("shows empty state when activities list is empty and not loading", () => {
        const wrapper = mountFeed({ activities: [], total: 0 });
        expect(wrapper.find('[data-testid="activity-empty"]').exists()).toBe(true);
        expect(wrapper.text()).toContain("No activity found");
    });

    it("renders one ActivityItem per entry", () => {
        const wrapper = mountFeed({
            activities: [makeEntry({ id: "ae-1" }), makeEntry({ id: "ae-2" })],
            total: 2,
        });
        expect(wrapper.findAll('[data-testid="activity-item"]')).toHaveLength(2);
    });

    it("shows activity feed container when activities are present", () => {
        const wrapper = mountFeed({ activities: [makeEntry()], total: 1 });
        expect(wrapper.find('[data-testid="activity-feed"]').exists()).toBe(true);
        expect(wrapper.find('[data-testid="activity-empty"]').exists()).toBe(false);
    });

    it("hides pagination when total <= pageSize", () => {
        const wrapper = mountFeed({
            activities: [makeEntry()],
            total: 5,
            filters: { pageSize: 20 },
        });
        expect(wrapper.text()).not.toContain("Previous");
        expect(wrapper.text()).not.toContain("Next");
    });

    it("shows pagination when total > pageSize", () => {
        const wrapper = mountFeed({
            activities: Array.from({ length: 20 }, (_, i) => makeEntry({ id: `ae-${i}` })),
            total: 50,
            filters: { pageSize: 20, page: 1 },
        });
        expect(wrapper.text()).toContain("Previous");
        expect(wrapper.text()).toContain("Next");
    });

    it("previous button is disabled on the first page", () => {
        const wrapper = mountFeed({
            activities: [makeEntry()],
            total: 50,
            filters: { page: 1, pageSize: 20 },
        });
        const prevButton = wrapper.findAll("button").find((b) => b.text() === "Previous");
        expect(prevButton?.attributes("disabled")).toBeDefined();
    });

    it("next button is disabled on the last page", () => {
        const wrapper = mountFeed({
            activities: [makeEntry()],
            total: 40,
            filters: { page: 2, pageSize: 20 },
        });
        const nextButton = wrapper.findAll("button").find((b) => b.text() === "Next");
        expect(nextButton?.attributes("disabled")).toBeDefined();
    });

    it("emits page-change with decremented page when previous is clicked", async () => {
        const wrapper = mountFeed({
            activities: [makeEntry()],
            total: 50,
            filters: { page: 2, pageSize: 20 },
        });
        const prevButton = wrapper.findAll("button").find((b) => b.text() === "Previous");
        await prevButton?.trigger("click");

        expect(wrapper.emitted("page-change")?.[0]?.[0]).toBe(1);
    });

    it("emits page-change with incremented page when next is clicked", async () => {
        const wrapper = mountFeed({
            activities: [makeEntry()],
            total: 50,
            filters: { page: 1, pageSize: 20 },
        });
        const nextButton = wrapper.findAll("button").find((b) => b.text() === "Next");
        await nextButton?.trigger("click");

        expect(wrapper.emitted("page-change")?.[0]?.[0]).toBe(2);
    });
});
