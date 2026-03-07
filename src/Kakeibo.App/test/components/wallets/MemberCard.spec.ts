import { describe, it, expect, vi, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import MemberCard from "@/components/wallets/MemberCard.vue";
import type { WalletMember } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const ownerMember: WalletMember = {
    userId: "owner-id",
    email: "owner@example.com",
    role: "Owner",
    joinedAt: "2026-03-01T00:00:00Z",
};

const regularMember: WalletMember = {
    userId: "member-id",
    email: "member@example.com",
    role: "Guest",
    joinedAt: "2026-03-02T00:00:00Z",
};

function mountCard(member: WalletMember, currentUserId: string, callerRole: string) {
    return mount(MemberCard, {
        props: { member, currentUserId, callerRole },
        global: { plugins: [i18n] },
    });
}

describe("MemberCard", () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    it("renders the member email", () => {
        const wrapper = mountCard(regularMember, "other-id", "Guest");
        expect(wrapper.text()).toContain("member@example.com");
    });

    it("shows the Owner badge when member is the wallet owner", () => {
        const wrapper = mountCard(ownerMember, "other-id", "Guest");
        expect(wrapper.text()).toContain("Owner");
    });

    it("does not show the Owner badge for a regular member", () => {
        const wrapper = mountCard(regularMember, "other-id", "Guest");
        expect(wrapper.text()).not.toContain("Owner");
    });

    it("shows 'Leave wallet' button when the current user views their own non-owner card", () => {
        const wrapper = mountCard(regularMember, "member-id", "Guest");
        expect(wrapper.text()).toContain("Leave wallet");
    });

    it("shows 'Remove' button when the owner views another member's card", () => {
        const wrapper = mountCard(regularMember, "owner-id", "Owner");
        expect(wrapper.text()).toContain("Remove");
    });

    it("does not show a remove button for the owner member card", () => {
        const wrapper = mountCard(ownerMember, "some-user-id", "Owner");
        expect(wrapper.text()).not.toContain("Leave wallet");
        expect(wrapper.text()).not.toContain("Remove");
    });

    it("does not show a remove button when a non-owner views another member", () => {
        const wrapper = mountCard(regularMember, "other-member-id", "Guest");
        expect(wrapper.text()).not.toContain("Remove");
        expect(wrapper.text()).not.toContain("Leave wallet");
    });

    it("emits remove event with the member userId on confirm", async () => {
        vi.spyOn(window, "confirm").mockReturnValue(true);
        const wrapper = mountCard(regularMember, "member-id", "Guest");
        const button = wrapper.find("button");
        await button.trigger("click");
        const emitted = wrapper.emitted("remove");
        expect(emitted).toBeDefined();
        expect(emitted![0]).toEqual(["member-id"]);
    });

    it("does not emit remove event when user cancels the confirm dialog", async () => {
        vi.spyOn(window, "confirm").mockReturnValue(false);
        const wrapper = mountCard(regularMember, "member-id", "Guest");
        const button = wrapper.find("button");
        await button.trigger("click");
        expect(wrapper.emitted("remove")).toBeUndefined();
    });
});
