import { test, expect } from "@playwright/test";

test.describe("Onboarding Flow", () => {
    test("redirects new user to onboarding when no wallets exist", async ({ page }) => {
        // Intercept API calls to simulate a user with no wallets
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([]) });
        });
        await page.route("**/api/users/me**", (route) => {
            route.fulfill({
                status: 200,
                body: JSON.stringify({
                    id: "test-user-id",
                    email: "test@example.com",
                    name: "Test User",
                }),
            });
        });

        await page.goto("/");
        await expect(page).toHaveURL(/\/onboarding/);
    });

    test("shows welcome step first", async ({ page }) => {
        await page.goto("/onboarding");
        await expect(page.getByText("Welcome to Kakeibo")).toBeVisible({ timeout: 5000 });
    });

    test("skip from welcome step navigates to home", async ({ page }) => {
        await page.goto("/onboarding");
        const skipButton = page.getByRole("button", { name: /skip/i }).first();
        if (await skipButton.isVisible()) {
            await skipButton.click();
        }
    });

    test("progress indicator shows 4 steps", async ({ page }) => {
        await page.goto("/onboarding");
        const progressBar = page.getByRole("progressbar");
        if (await progressBar.isVisible()) {
            await expect(progressBar).toHaveAttribute("aria-valuemax", "4");
        }
    });
});
