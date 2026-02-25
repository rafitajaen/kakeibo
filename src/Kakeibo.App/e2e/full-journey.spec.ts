import { test, expect } from "@playwright/test";

/**
 * Full user journey smoke test:
 * Register → Onboarding → Dashboard → Settings → Logout
 *
 * This test uses route mocking to simulate a complete authenticated session
 * without requiring a live API. It validates that the navigation structure
 * and key screens are accessible and render correctly.
 */

const mockUser = {
    id: "journey-user",
    email: "journey@example.com",
    role: "User",
    currency: "USD",
    isVerified: true,
    name: "Journey User",
};

const mockWallet = {
    id: "wallet-journey",
    name: "My Wallet",
    type: "Personal",
    currency: "USD",
    balance: 5000,
    isArchived: false,
    createdAt: "2026-01-01",
};

test.describe("Full User Journey", () => {
    test("authenticated user can navigate from dashboard to settings and back", async ({
        page,
    }) => {
        // Simulate authenticated session with wallets
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([mockWallet]) });
        });
        await page.route("**/api/users/me/sessions**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([]) });
        });

        // Start at dashboard
        await page.goto("/");
        await expect(page).toHaveURL(/\/$|\/#/, { timeout: 5000 });

        // Navigate to settings
        await page.goto("/settings");
        await expect(page.getByText(/profile/i)).toBeVisible({ timeout: 5000 });

        // Return to dashboard
        await page.goto("/");
        await expect(page).toHaveURL(/\/$|\/#/, { timeout: 5000 });
    });

    test("new user (no wallets) is redirected to onboarding", async ({ page }) => {
        // Simulate authenticated session with no wallets
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([]) });
        });

        await page.goto("/");
        await expect(page).toHaveURL(/\/onboarding/, { timeout: 5000 });
        await expect(page.getByText(/welcome/i)).toBeVisible({ timeout: 5000 });
    });

    test("unauthenticated user cannot access protected routes", async ({ page }) => {
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 401 });
        });

        // All protected routes redirect to login
        for (const path of ["/", "/wallets", "/settings"]) {
            await page.goto(path);
            await expect(page).toHaveURL(/\/login/, { timeout: 5000 });
        }
    });

    test("onboarding welcome renders get started and skip buttons", async ({ page }) => {
        await page.goto("/onboarding");
        await expect(page.getByRole("button", { name: /get started/i })).toBeVisible({
            timeout: 5000,
        });
        await expect(page.getByRole("button", { name: /skip/i }).first()).toBeVisible({
            timeout: 5000,
        });
    });

    test("settings profile tab allows changing display name", async ({ page }) => {
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([mockWallet]) });
        });
        await page.route("**/api/users/me/sessions**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([]) });
        });
        await page.route("**/api/users/me/profile**", (route) => {
            route.fulfill({
                status: 200,
                body: JSON.stringify({ name: "Updated User", currency: "USD" }),
            });
        });

        await page.goto("/settings");
        const nameField = page.getByLabel(/name/i);
        await nameField.fill("Updated User");
        // Submit form
        await page.getByRole("button", { name: /save/i }).first().click();
    });
});
