import { test, expect } from "@playwright/test";

test.describe("Authentication", () => {
    test("unauthenticated user is redirected to login", async ({ page }) => {
        // Simulate no active session
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 401 });
        });

        await page.goto("/");
        await expect(page).toHaveURL(/\/login/, { timeout: 5000 });
    });

    test("login page renders form fields", async ({ page }) => {
        await page.goto("/login");
        await expect(page.getByLabel(/email/i)).toBeVisible({ timeout: 5000 });
        await expect(page.getByLabel(/password/i)).toBeVisible({ timeout: 5000 });
    });

    test("register page renders form fields", async ({ page }) => {
        await page.goto("/register");
        await expect(page.getByLabel(/email/i)).toBeVisible({ timeout: 5000 });
    });

    test("guest route redirects authenticated user to home", async ({ page }) => {
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({
                status: 200,
                body: JSON.stringify({
                    id: "test-user",
                    email: "test@example.com",
                    role: "User",
                    currency: "USD",
                    isVerified: true,
                    name: null,
                }),
            });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({
                status: 200,
                body: JSON.stringify([
                    {
                        id: "1",
                        name: "Checking",
                        type: "Personal",
                        currency: "USD",
                        balance: 100,
                        isArchived: false,
                        createdAt: "2026-01-01",
                    },
                ]),
            });
        });

        await page.goto("/login");
        await expect(page).toHaveURL(/\/$|\/onboarding/, { timeout: 5000 });
    });
});
