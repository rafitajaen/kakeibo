import { test, expect } from "@playwright/test";

test.describe("Transactions", () => {
    test("unauthenticated user is redirected to login when accessing transactions page", async ({
        page,
    }) => {
        // The router guard redirects unauthenticated users to /login for requiresAuth routes.
        await page.goto("/wallets/test-wallet-id/transactions");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing record transaction page", async ({
        page,
    }) => {
        await page.goto("/wallets/test-wallet-id/transactions/new");
        await expect(page).toHaveURL(/\/login/);
    });

    // Full flow test (requires a running API + seeded user + wallet).
    // Run manually with: bun run test:e2e --grep "record income"
    //
    // test("record income → appears in transaction list", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/wallets/<wallet-id>/transactions/new");
    //   await page.getByLabel("Type").selectOption("Income");
    //   // ... fill amount, description, date, category
    //   await page.getByRole("button", { name: "Record transaction" }).click();
    //   await expect(page).toHaveURL(/\/transactions$/);
    //   await expect(page.getByText("Salary")).toBeVisible();
    // });
});
