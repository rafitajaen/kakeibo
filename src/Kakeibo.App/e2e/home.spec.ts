import { test, expect } from "@playwright/test";

test.describe("Dashboard", () => {
    test("should load and display dashboard sections", async ({ page }) => {
        await page.goto("/");

        // The dashboard renders section headings for all key areas.
        // These are visible once data is loaded (or on empty state).
        await expect(page.getByText(/balance overview/i)).toBeVisible();
        await expect(page.getByText(/recent transactions/i)).toBeVisible();
        await expect(page.getByText(/budget summary/i)).toBeVisible();
        await expect(page.getByText(/goal summary/i)).toBeVisible();
        await expect(page.getByText(/quick actions/i)).toBeVisible();
    });
});
