import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useCategoriesStore } from "@/stores/categories";

// Mock the axios instance so no real HTTP requests are made.
vi.mock("@/lib/axios", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    },
}));

import api from "@/lib/axios";

const apiMock = api as unknown as {
    get: ReturnType<typeof vi.fn>;
    post: ReturnType<typeof vi.fn>;
    put: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
};

const systemCategory = {
    id: "10000000-0000-0000-0000-000000000001",
    name: "Housing",
    isSystem: true,
    isArchived: false,
};

const customCategory = {
    id: "custom-1",
    name: "Hobbies",
    isSystem: false,
    isArchived: false,
};

const archivedCategory = {
    id: "custom-2",
    name: "Old hobby",
    isSystem: false,
    isArchived: true,
};

describe("useCategoriesStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("fetchCategories populates the list", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [systemCategory, customCategory] });
        const store = useCategoriesStore();

        await store.fetchCategories();

        expect(store.categories).toHaveLength(2);
        expect(store.isLoading).toBe(false);
    });

    it("fetchCategories passes includeArchived param", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [] });
        const store = useCategoriesStore();

        await store.fetchCategories(true);

        expect(apiMock.get).toHaveBeenCalledWith("/api/categories", {
            params: { includeArchived: true },
        });
    });

    it("systemCategories computed returns only system categories", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [systemCategory, customCategory] });
        const store = useCategoriesStore();
        await store.fetchCategories();

        expect(store.systemCategories).toHaveLength(1);
        expect(store.systemCategories[0].id).toBe(systemCategory.id);
    });

    it("customCategories computed returns only non-system categories", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [systemCategory, customCategory] });
        const store = useCategoriesStore();
        await store.fetchCategories();

        expect(store.customCategories).toHaveLength(1);
        expect(store.customCategories[0].id).toBe("custom-1");
    });

    it("activeCategories computed excludes archived categories", async () => {
        apiMock.get.mockResolvedValueOnce({
            data: [systemCategory, customCategory, archivedCategory],
        });
        const store = useCategoriesStore();
        await store.fetchCategories(true);

        expect(store.activeCategories).toHaveLength(2);
        expect(store.activeCategories.every((c) => !c.isArchived)).toBe(true);
    });

    it("createCategory appends the new category to the list", async () => {
        const newCategory = { id: "custom-3", name: "Travel", isSystem: false, isArchived: false };
        apiMock.post.mockResolvedValueOnce({ data: newCategory });
        const store = useCategoriesStore();

        const result = await store.createCategory({ name: "Travel" });

        expect(result).toEqual(newCategory);
        expect(store.categories).toHaveLength(1);
        expect(store.categories[0].name).toBe("Travel");
        expect(apiMock.post).toHaveBeenCalledWith("/api/categories", { name: "Travel" });
    });

    it("updateCategory replaces the entry in the list", async () => {
        const updated = { ...customCategory, name: "Gaming" };
        apiMock.get.mockResolvedValueOnce({ data: [customCategory] });
        apiMock.put.mockResolvedValueOnce({ data: updated });
        const store = useCategoriesStore();
        await store.fetchCategories();

        await store.updateCategory("custom-1", { name: "Gaming" });

        expect(store.categories[0].name).toBe("Gaming");
        expect(apiMock.put).toHaveBeenCalledWith("/api/categories/custom-1", { name: "Gaming" });
    });

    it("archiveCategory marks the category as archived in the list", async () => {
        apiMock.get.mockResolvedValueOnce({ data: [customCategory] });
        apiMock.delete.mockResolvedValueOnce({});
        const store = useCategoriesStore();
        await store.fetchCategories();

        await store.archiveCategory("custom-1");

        expect(store.categories[0].isArchived).toBe(true);
        expect(apiMock.delete).toHaveBeenCalledWith("/api/categories/custom-1");
    });
});
