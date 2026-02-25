import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import { createRouter, createMemoryHistory } from "vue-router";
import RecurringList from "@/components/recurring/RecurringList.vue";
import type { RecurringPattern } from "@/stores/recurring";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });
const router = createRouter({ history: createMemoryHistory(), routes: [] });

const makePattern = (overrides: Partial<RecurringPattern> = {}): RecurringPattern => ({
    id: "pattern-1",
    name: "Monthly Rent",
    amount: 1200,
    description: "Apartment rent",
    transactionType: "Expense",
    frequency: "Monthly",
    categoryId: "cat-1",
    categoryName: "Housing",
    walletId: "wallet-1",
    walletName: "Checking",
    destinationWalletId: null,
    destinationWalletName: null,
    startDate: "2026-01-01",
    endDate: null,
    nextOccurrence: "2026-04-01",
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
});

function mountList(patterns: RecurringPattern[]) {
    return mount(RecurringList, {
        props: { patterns },
        global: {
            plugins: [i18n, createPinia(), router],
            stubs: {
                // Stub AlertDialog components to avoid portal/teleport issues in jsdom
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

describe("RecurringList", () => {
    it("renders pattern name and category name", () => {
        const wrapper = mountList([makePattern()]);
        expect(wrapper.text()).toContain("Monthly Rent");
        expect(wrapper.text()).toContain("Housing");
    });

    it("renders formatted nextOccurrence date", () => {
        const wrapper = mountList([makePattern({ nextOccurrence: "2026-04-01" })]);
        // date-fns 'PP' format: Apr 1, 2026
        expect(wrapper.text()).toContain("Apr 1, 2026");
    });

    it("renders the wallet name", () => {
        const wrapper = mountList([makePattern({ walletName: "Checking" })]);
        expect(wrapper.text()).toContain("Checking");
    });

    it("renders amount formatted to 2 decimal places", () => {
        const wrapper = mountList([makePattern({ amount: 1200 })]);
        expect(wrapper.text()).toContain("1200.00");
    });

    it("emits delete event when confirm button is clicked", async () => {
        const wrapper = mountList([makePattern()]);
        const confirmButton = wrapper.find('[data-testid="delete-confirm"]');
        await confirmButton.trigger("click");
        expect(wrapper.emitted("delete")).toBeTruthy();
        expect(wrapper.emitted("delete")?.[0]?.[0]).toBe("pattern-1");
    });

    it("renders one item per pattern with data-testid", () => {
        const patterns = [makePattern(), makePattern({ id: "pattern-2", name: "Weekly Gym" })];
        const wrapper = mountList(patterns);
        expect(wrapper.findAll('[data-testid="pattern-item"]')).toHaveLength(2);
    });

    it("renders edit button for each pattern", () => {
        const wrapper = mountList([makePattern()]);
        const editButton = wrapper.findAll("button").find((b) => b.text() === "Edit");
        expect(editButton).toBeDefined();
    });
});
