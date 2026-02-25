import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import RecurringForm from "@/components/recurring/RecurringForm.vue";
import type { RecurringPattern } from "@/stores/recurring";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const mockCategories: Category[] = [
    { id: "cat-1", name: "Housing", isSystem: true, isArchived: false },
    { id: "cat-2", name: "Food", isSystem: false, isArchived: false },
];

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
    {
        id: "wallet-2",
        name: "Savings",
        type: "Personal",
        currency: "EUR",
        balance: 5000,
        isArchived: false,
        createdAt: "2026-01-01T00:00:00Z",
    },
];

const mockPattern: RecurringPattern = {
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
};

// Shared stubs for shadcn-vue Select components to avoid portal/jsdom issues
const selectStubs = {
    Select: {
        template: '<div class="select-stub"><slot /></div>',
        props: ["modelValue"],
        emits: ["update:modelValue"],
    },
    SelectTrigger: { template: "<div />" },
    SelectContent: { template: "<div><slot /></div>" },
    SelectItem: { template: "<div />", props: ["value"] },
    SelectValue: { template: "<div />" },
};

function mountForm(props: {
    pattern?: RecurringPattern;
    isSubmitting?: boolean;
    categories?: Category[];
    wallets?: Wallet[];
}) {
    return mount(RecurringForm, {
        props: {
            pattern: props.pattern,
            categories: props.categories ?? mockCategories,
            wallets: props.wallets ?? mockWallets,
            isSubmitting: props.isSubmitting,
        },
        global: {
            plugins: [i18n, createPinia()],
            stubs: selectStubs,
        },
    });
}

describe("RecurringForm", () => {
    it("renders name input field in create mode", () => {
        const wrapper = mountForm({});
        const nameInput = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(nameInput.exists()).toBe(true);
    });

    it("renders amount input field", () => {
        const wrapper = mountForm({});
        const amountInput = wrapper.find<HTMLInputElement>('input[type="number"]');
        expect(amountInput.exists()).toBe(true);
    });

    it("renders startDate input in create mode as editable", () => {
        const wrapper = mountForm({});
        const dateInput = wrapper.find<HTMLInputElement>('input[type="date"]');
        expect(dateInput.exists()).toBe(true);
        expect(dateInput.attributes("readonly")).toBeUndefined();
    });

    it("renders startDate input as readonly in edit mode", () => {
        const wrapper = mountForm({ pattern: mockPattern });
        // First date input is startDate
        const dateInputs = wrapper.findAll<HTMLInputElement>('input[type="date"]');
        const startDateInput = dateInputs[0];
        expect(startDateInput.attributes("readonly")).toBeDefined();
    });

    it("shows 'Create pattern' submit button in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Create pattern");
    });

    it("shows 'Save changes' submit button in edit mode", () => {
        const wrapper = mountForm({ pattern: mockPattern });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("pre-fills name input in edit mode", () => {
        const wrapper = mountForm({ pattern: mockPattern });
        const nameInput = wrapper.find<HTMLInputElement>('input[type="text"]');
        expect(nameInput.element.value).toBe("Monthly Rent");
    });

    it("pre-fills amount input in edit mode", () => {
        const wrapper = mountForm({ pattern: mockPattern });
        const amountInput = wrapper.find<HTMLInputElement>('input[type="number"]');
        expect(Number(amountInput.element.value)).toBe(1200);
    });

    it("disables submit button when isSubmitting is true", () => {
        const wrapper = mountForm({ isSubmitting: true });
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeDefined();
    });

    it("submit button is enabled by default", () => {
        const wrapper = mountForm({});
        const button = wrapper.find('button[type="submit"]');
        expect(button.attributes("disabled")).toBeUndefined();
    });

    it("does not show destinationWallet field when transactionType is Expense", () => {
        // Default transactionType is Expense — destinationWallet field should not render
        const wrapper = mountForm({});
        expect(wrapper.text()).not.toContain("Destination wallet");
    });

    it("renders a form element", () => {
        const wrapper = mountForm({});
        expect(wrapper.find("form").exists()).toBe(true);
    });
});
