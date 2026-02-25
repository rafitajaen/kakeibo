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

test.describe("Wallets", () => {
    test.beforeEach(async ({ page }) => {
        await page.route("**/api/auth/me**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify(mockUser) });
        });
        await page.route("**/api/wallets**", (route) => {
            route.fulfill({ status: 200, body: JSON.stringify([mockWallet]) });
        });
    });

    test("wallets page renders wallet list", async ({ page }) => {
        await page.goto("/wallets");
        await expect(page.getByText(/wallets/i)).toBeVisible({ timeout: 5000 });
    });

    test("wallets page shows create wallet button", async ({ page }) => {
        await page.goto("/wallets");
        await expect(
            page.getByRole("button", { name: /new wallet|create wallet|add wallet/i }),
        ).toBeVisible({
            timeout: 5000,
        });
    });

    test("wallet list displays wallet name", async ({ page }) => {
        await page.goto("/wallets");
        await expect(page.getByText("Checking")).toBeVisible({ timeout: 5000 });
    });

    test("clicking create wallet opens dialog or navigates to create form", async ({ page }) => {
        await page.goto("/wallets");
        const createBtn = page.getByRole("button", {
            name: /new wallet|create wallet|add wallet/i,
        });
        await createBtn.click();
        // Either a dialog opens or we navigate to a create page
        const dialogOrForm = page.locator("[role='dialog'], form").first();
        await expect(dialogOrForm).toBeVisible({ timeout: 5000 });
    });
});
