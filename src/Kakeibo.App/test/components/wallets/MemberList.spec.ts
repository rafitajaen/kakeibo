import { describe, it, expect, vi, afterEach } from "vitest";
import { mount } from "@vue/test-utils";
import { createI18n } from "vue-i18n";
import MemberList from "@/components/wallets/MemberList.vue";
import type { WalletMember } from "@/stores/wallets";
import en from "@/locales/en.json";

const i18n = createI18n({ legacy: false, locale: "en", messages: { en } });

const owner: WalletMember = {
    userId: "owner-id",
    email: "owner@example.com",
    isOwner: true,
    joinedAt: "2026-03-01T00:00:00Z",
};

const member: WalletMember = {
    userId: "member-id",
    email: "member@example.com",
    isOwner: false,
    joinedAt: "2026-03-02T00:00:00Z",
};

function mountList(members: WalletMember[], currentUserId = "owner-id", isOwner = true) {
    return mount(MemberList, {
        props: { members, currentUserId, isOwner },
        global: { plugins: [i18n] },
    });
}

describe("MemberList", () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    it("shows the empty state when there are no members", () => {
        const wrapper = mountList([]);
        expect(wrapper.text()).toContain("No members yet.");
    });

    it("renders an entry for each member", () => {
        const wrapper = mountList([owner, member]);
        expect(wrapper.text()).toContain("owner@example.com");
        expect(wrapper.text()).toContain("member@example.com");
    });

    it("does not show the empty state when members are present", () => {
        const wrapper = mountList([owner]);
        expect(wrapper.text()).not.toContain("No members yet.");
    });

    it("bubbles the remove event from a MemberCard", async () => {
        vi.spyOn(window, "confirm").mockReturnValue(true);
        const wrapper = mountList([member], "owner-id", true);
        // The Remove button is inside the nested MemberCard.
        const button = wrapper.find("button");
        await button.trigger("click");
        const emitted = wrapper.emitted("remove");
        expect(emitted).toBeDefined();
        expect(emitted![0]).toEqual(["member-id"]);
    });
});
