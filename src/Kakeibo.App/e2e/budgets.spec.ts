import { test, expect } from "@playwright/test";

test.describe("Budgets", () => {
    test("unauthenticated user is redirected to login when accessing budgets page", async ({
        page,
    }) => {
        // The router guard redirects unauthenticated users to /login for requiresAuth routes.
        await page.goto("/budgets");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing create budget page", async ({
        page,
    }) => {
        await page.goto("/budgets/new");
        await expect(page).toHaveURL(/\/login/);
    });

    test("unauthenticated user is redirected to login when accessing edit budget page", async ({
        page,
    }) => {
        await page.goto("/budgets/test-budget-id/edit");
        await expect(page).toHaveURL(/\/login/);
    });

    // Full flow tests (require a running API + seeded user + wallet + category).
    // Run manually with: bun run test:e2e --grep "budget"
    //
    // test("budgets list is empty for a new user", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/budgets");
    //   await expect(page.getByText(/no budgets/i)).toBeVisible();
    // });
    //
    // test("create budget → appears in list with correct status badge", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/budgets/new");
    //   // Fill the budget form
    //   await page.getByLabel("Category").selectOption("Food & Dining");
    //   await page.getByLabel("Amount limit").fill("400");
    //   await page.getByRole("button", { name: "Create budget" }).click();
    //   await expect(page).toHaveURL(/\/budgets$/);
    //   await expect(page.getByText("Food & Dining")).toBeVisible();
    //   await expect(page.getByText(/on track/i)).toBeVisible();
    // });
    //
    // test("edit budget → changes are saved", async ({ page }) => {
    //   // Navigate to an existing budget's edit page by id
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/budgets");
    //   await page.getByRole("button", { name: "Edit" }).first().click();
    //   await expect(page).toHaveURL(/\/budgets\/.+\/edit/);
    //   await page.getByLabel("Amount limit").clear();
    //   await page.getByLabel("Amount limit").fill("500");
    //   await page.getByRole("button", { name: "Save changes" }).click();
    //   await expect(page).toHaveURL(/\/budgets$/);
    // });
    //
    // test("delete budget → disappears from list", async ({ page }) => {
    //   await page.goto("/login");
    //   await page.getByLabel("Email").fill("test@example.com");
    //   await page.getByLabel("Password").fill("Password1");
    //   await page.getByRole("button", { name: "Sign in" }).click();
    //   await page.goto("/budgets");
    //   const initialCount = await page.getByTestId("budget-item").count();
    //   page.once("dialog", (dialog) => dialog.accept());
    //   await page.getByRole("button", { name: "Delete" }).first().click();
    //   await expect(page.getByTestId("budget-item")).toHaveCount(initialCount - 1);
    // });
});
