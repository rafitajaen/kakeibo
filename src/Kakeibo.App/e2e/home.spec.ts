import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
    test("should load and display welcome message", async ({ page }) => {
        await page.goto("/");

        // Check that the welcome heading is visible
        await expect(page.getByRole("heading", { name: /welcome to kakeibo/i })).toBeVisible();

        // Check that the tagline is visible
        await expect(page.getByText(/mindful money management/i)).toBeVisible();
    });
});
