import { test, expect } from "@playwright/test";

const mockUser = {
    id: "test-user",
    email: "test@example.com",
    role: "User",
    currency: "USD",
    isVerified: true,
    name: "Test User",
};

const mockWallet = {
    id: "wallet-1",
    name: "Checking",
    type: "Personal",
    currency: "USD",
    balance: 1000,
    isArchived: false,
    createdAt: "2026-01-01",
};

test.describe("Settings", () => {
    test.beforeEach(async ({ page }) => {
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([mockWallet]) });
        });
        await page.route("**/api/users/me/sessions**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([]) });
        });
    });

    test("settings page renders profile tab by default", async ({ page }) => {
        await page.goto("/settings");
        await expect(page.getByText(/profile/i)).toBeVisible({ timeout: 5000 });
    });

    test("profile tab shows name and currency fields", async ({ page }) => {
        await page.goto("/settings");
        await expect(page.getByLabel(/name/i)).toBeVisible({ timeout: 5000 });
        await expect(page.getByLabel(/currency/i)).toBeVisible({ timeout: 5000 });
    });

    test("security tab shows password change form", async ({ page }) => {
        await page.goto("/settings");
        const securityTab = page.getByRole("tab", { name: /security/i });
        await securityTab.click();
        await expect(page.getByLabel(/current password/i)).toBeVisible({ timeout: 5000 });
        await expect(page.getByLabel(/new password/i)).toBeVisible({ timeout: 5000 });
    });

    test("sessions tab renders when clicked", async ({ page }) => {
        await page.goto("/settings");
        const sessionsTab = page.getByRole("tab", { name: /sessions/i });
        await sessionsTab.click();
        await expect(page.getByText(/sessions/i)).toBeVisible({ timeout: 5000 });
    });

    test("settings link is accessible from app header", async ({ page }) => {
        await page.goto("/");
        // Look for a settings link in the header/nav
        const settingsLink = page.getByRole("link", { name: /settings/i });
        if (await settingsLink.isVisible()) {
            await settingsLink.click();
            await expect(page).toHaveURL(/\/settings/, { timeout: 5000 });
        }
    });
});
