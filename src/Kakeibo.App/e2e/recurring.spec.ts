import { test, expect } from "@playwright/test";

test.describe("Recurring Patterns", () => {
    test("unauthenticated user is redirected to login when accessing recurring page", async ({
        page,
    }) => {
        // The router guard redirects unauthenticated users to /login for requiresAuth routes.
        await page.goto("/recurring");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing create pattern page", async ({
        page,
    }) => {
        await page.goto("/recurring/new");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing edit pattern page", async ({
        page,
    }) => {
        await page.goto("/recurring/test-pattern-id/edit");
        await expect(page).toHaveURL(/\/login/);
    });

    // Full flow tests — require a running API + seeded user + wallet + category.
    // Run manually: bun run app:test:e2e --grep "recurring"

    test("recurring list is empty for a new user", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Email").fill("test@example.com");
        await page.getByLabel("Password").fill("Password1");
        await page.getByRole("button", { name: "Sign in" }).click();
        await page.goto("/recurring");
        await expect(page.getByText(/no recurring patterns/i)).toBeVisible();
    });
});
