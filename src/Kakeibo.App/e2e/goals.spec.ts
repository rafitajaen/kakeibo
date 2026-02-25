import { test, expect } from "@playwright/test";

test.describe("Goals", () => {
    test("unauthenticated user is redirected to login when accessing goals page", async ({
        page,
    }) => {
        // The router guard redirects unauthenticated users to /login for requiresAuth routes.
        await page.goto("/goals");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing create goal page", async ({
        page,
    }) => {
        await page.goto("/goals/new");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing edit goal page", async ({
        page,
    }) => {
        await page.goto("/goals/test-goal-id/edit");
        await expect(page).toHaveURL(/\/login/);
    });

    // Full flow tests (require a running API + seeded user + wallet).
    // Run manually with: bun run test:e2e --grep "goal"
    //
    // test("goals list is empty for a new user", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/goals");
    //   await expect(page.getByText(/no goals/i)).toBeVisible();
    // });
    //
    // test("create goal → appears in list with correct status badge", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/goals/new");
    //   await page.getByLabel("Name").fill("Europe Vacation");
    //   await page.getByLabel("Target amount").fill("5000");
    //   // Select wallet
    //   await page.getByRole("button", { name: "Select a wallet" }).click();
    //   await page.getByRole("option").first().click();
    //   await page.getByRole("button", { name: "Create goal" }).click();
    //   await expect(page).toHaveURL(/\/goals$/);
    //   await expect(page.getByText("Europe Vacation")).toBeVisible();
    //   await expect(page.getByText(/not started/i)).toBeVisible();
    // });
    //
    // test("edit goal → changes are saved", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/goals");
    //   await page.getByRole("button", { name: "Edit" }).first().click();
    //   await expect(page).toHaveURL(/\/goals\/.+\/edit/);
    //   await page.getByLabel("Target amount").clear();
    //   await page.getByLabel("Target amount").fill("8000");
    //   await page.getByRole("button", { name: "Save changes" }).click();
    //   await expect(page).toHaveURL(/\/goals$/);
    // });
    //
    // test("delete goal → disappears from list", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/goals");
    //   const initialCount = await page.getByTestId("goal-item").count();
    //   page.once("dialog", (dialog) => dialog.accept());
    //   await page.getByRole("button", { name: "Delete" }).first().click();
    //   await expect(page.getByTestId("goal-item")).toHaveCount(initialCount - 1);
    // });
});
