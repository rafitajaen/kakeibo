import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import TransactionAttachmentList from "@/components/transactions/TransactionAttachmentList.vue";
import type { TransactionAttachment } from "@/stores/transactions";
import en from "@/locales/en.json";

vi.mock("@/lib/axios");

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const makeAttachment = (overrides: Partial<TransactionAttachment> = {}): TransactionAttachment => ({
    id: "att-1",
    transactionId: "tx-1",
    fileName: "receipt.jpg",
    contentType: "image/jpeg",
    fileSizeBytes: 102400,
    uploadedByUserId: "user-1",
    createdAt: "2026-02-15T12:00:00Z",
    ...overrides,
});

const mountComponent = (props: {
    transactionId?: string;
    attachments?: TransactionAttachment[];
    isLoading?: boolean;
}) =>
    mount(TransactionAttachmentList, {
        props: {
            transactionId: props.transactionId ?? "tx-1",
            attachments: props.attachments ?? [],
            isLoading: props.isLoading ?? false,
        },
        global: { plugins: [i18n] },
    });

describe("TransactionAttachmentList", () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("shows empty state when there are no attachments", () => {
        const wrapper = mountComponent({ attachments: [] });
        expect(wrapper.text()).toContain("No attachments yet");
    });

    it("renders attachment name and formatted size for each item", () => {
        const attachments = [
            makeAttachment({ id: "att-1", fileName: "invoice.pdf", fileSizeBytes: 1024 * 1024 }),
            makeAttachment({ id: "att-2", fileName: "receipt.png", fileSizeBytes: 512 }),
        ];
        const wrapper = mountComponent({ attachments });

        expect(wrapper.text()).toContain("invoice.pdf");
        expect(wrapper.text()).toContain("1.0 MB");
        expect(wrapper.text()).toContain("receipt.png");
        expect(wrapper.text()).toContain("512 B");
    });

    it("emits download event when download button is clicked", async () => {
        const attachment = makeAttachment();
        const wrapper = mountComponent({ attachments: [attachment] });

        // First button in the list item is the download button
        const buttons = wrapper.findAll("button[title]");
        const downloadBtn = buttons.find((b) => b.attributes("title") === "Download");
        await downloadBtn?.trigger("click");

        expect(wrapper.emitted("download")).toBeTruthy();
        expect(wrapper.emitted("download")?.[0]).toEqual([attachment]);
    });

    it("emits delete event with attachment id when delete button is clicked", async () => {
        const attachment = makeAttachment({ id: "att-42" });
        const wrapper = mountComponent({ attachments: [attachment] });

        const buttons = wrapper.findAll("button[title]");
        const deleteBtn = buttons.find((b) => b.attributes("title") === "Delete");
        await deleteBtn?.trigger("click");

        expect(wrapper.emitted("delete")).toBeTruthy();
        expect(wrapper.emitted("delete")?.[0]).toEqual(["att-42"]);
    });
});
