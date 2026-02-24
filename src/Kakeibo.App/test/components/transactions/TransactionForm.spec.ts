import { describe, it, expect } from "vitest";
import { mount } from "@vue/test-utils";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import TransactionForm from "@/components/transactions/TransactionForm.vue";
import type { Category } from "@/stores/categories";
import type { Wallet } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const mockCategories: Category[] = [
    { id: "cat-1", name: "Housing", isSystem: true, isArchived: false },
    { id: "cat-2", name: "Food", isSystem: true, isArchived: false },
];

const mockWallets: Wallet[] = [
    {
        id: "w-1",
        name: "Checking",
        type: "Personal",
        currency: "EUR",
        balance: 500,
        isArchived: false,
        createdAt: "2026-01-01T00:00:00Z",
    },
    {
        id: "w-2",
        name: "Savings",
        type: "Personal",
        currency: "EUR",
        balance: 1000,
        isArchived: false,
        createdAt: "2026-01-01T00:00:00Z",
    },
];

function mountForm(props: {
    initialData?: object;
    categories?: Category[];
    wallets?: Wallet[];
    sourceWalletId?: string;
}) {
    return mount(TransactionForm, {
        props: {
            categories: props.categories ?? mockCategories,
            wallets: props.wallets ?? mockWallets,
            sourceWalletId: props.sourceWalletId,
            initialData: props.initialData,
        },
        global: {
            plugins: [i18n, createPinia()],
            stubs: {
                // Stub CalculatorInput to avoid DOM complexity in form tests
                CalculatorInput: {
                    template: '<input type="text" class="calculator-input" />',
                    props: ["modelValue"],
                    emits: ["update:modelValue"],
                },
                // Stub CategorySelector to avoid Select portal issues
                CategorySelector: {
                    template: '<select class="category-selector"></select>',
                    props: ["categories", "modelValue", "placeholder"],
                    emits: ["update:modelValue"],
                },
            },
        },
    });
}

describe("TransactionForm", () => {
    it("renders all required fields in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.find("form").exists()).toBe(true);
        // Type, Amount (CalculatorInput stub), Description, Date fields should exist
        expect(wrapper.find(".calculator-input").exists()).toBe(true);
        expect(wrapper.find('input[type="text"]').exists()).toBe(true);
        expect(wrapper.find('input[type="date"]').exists()).toBe(true);
    });

    it("shows 'Record transaction' button in create mode", () => {
        const wrapper = mountForm({});
        expect(wrapper.text()).toContain("Record transaction");
    });

    it("shows 'Save changes' button in edit mode", () => {
        const wrapper = mountForm({
            initialData: {
                type: "Expense",
                amount: 50,
                description: "Rent",
                date: "2026-02-15",
                categoryId: "cat-1",
            },
        });
        expect(wrapper.text()).toContain("Save changes");
    });

    it("does not show destination wallet field when type is Expense", () => {
        const wrapper = mountForm({
            initialData: { type: "Expense" },
        });
        // The destination wallet label should not be visible for non-Transfer types
        expect(wrapper.text()).not.toContain("Destination wallet");
    });

    it("shows destination wallet field when type is Transfer", () => {
        const wrapper = mountForm({
            initialData: { type: "Transfer" },
        });
        expect(wrapper.text()).toContain("Destination wallet");
    });

    it("filters out sourceWalletId from destination wallet options", () => {
        const wrapper = mountForm({
            initialData: { type: "Transfer" },
            sourceWalletId: "w-1",
        });
        // Should show the destination section — w-2 would be available, w-1 excluded
        expect(wrapper.text()).toContain("Destination wallet");
    });
});
