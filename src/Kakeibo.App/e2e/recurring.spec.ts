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

    test("create recurring pattern → appears in list", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Email").fill("test@example.com");
        await page.getByLabel("Password").fill("Password1");
        await page.getByRole("button", { name: "Sign in" }).click();
        await page.goto("/recurring/new");

        // Fill text inputs
        await page.getByLabel("Name").fill("Monthly Rent");
        await page.getByLabel("Description").fill("Apartment rent");
        await page.getByLabel("Amount").fill("1200");

        // Select transaction type
        await page.getByRole("button", { name: "Select a type" }).click();
        await page.getByRole("option", { name: "Expense" }).click();

        // Select frequency
        await page.getByRole("button", { name: "Select a frequency" }).click();
        await page.getByRole("option", { name: "Monthly" }).click();

        // Select category
        await page.getByRole("button", { name: "Select a category" }).click();
        await page.getByRole("option").first().click();

        // Select wallet
        await page.getByRole("button", { name: "Select a wallet" }).click();
        await page.getByRole("option").first().click();

        await page.getByRole("button", { name: "Create pattern" }).click();
        await expect(page).toHaveURL(/\/recurring$/);
        await expect(page.getByText("Monthly Rent")).toBeVisible();
    });

    test("edit recurring pattern → changes are saved", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Email").fill("test@example.com");
        await page.getByLabel("Password").fill("Password1");
        await page.getByRole("button", { name: "Sign in" }).click();
        await page.goto("/recurring");
        await page.getByRole("button", { name: "Edit" }).first().click();
        await expect(page).toHaveURL(/\/recurring\/.+\/edit/);
        await page.getByLabel("Amount").clear();
        await page.getByLabel("Amount").fill("1500");
        await page.getByRole("button", { name: "Save changes" }).click();
        await expect(page).toHaveURL(/\/recurring$/);
    });

    test("delete recurring pattern → disappears from list", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Email").fill("test@example.com");
        await page.getByLabel("Password").fill("Password1");
        await page.getByRole("button", { name: "Sign in" }).click();
        await page.goto("/recurring");
        const initialCount = await page.getByTestId("pattern-item").count();
        // Open delete confirmation dialog
        await page
            .getByTestId("pattern-item")
            .first()
            .getByRole("button", { name: "Delete" })
            .click();
        // Confirm deletion
        await page.getByTestId("delete-confirm").click();
        await expect(page.getByTestId("pattern-item")).toHaveCount(initialCount - 1);
    });

    test("forecast tab shows upcoming transactions after creating a pattern", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Email").fill("test@example.com");
        await page.getByLabel("Password").fill("Password1");
        await page.getByRole("button", { name: "Sign in" }).click();

        // Create a daily pattern so it always appears in the 30-day forecast
        await page.goto("/recurring/new");
        await page.getByLabel("Name").fill("Daily Coffee");
        await page.getByLabel("Description").fill("Morning coffee");
        await page.getByLabel("Amount").fill("5");

        await page.getByRole("button", { name: "Select a type" }).click();
        await page.getByRole("option", { name: "Expense" }).click();

        await page.getByRole("button", { name: "Select a frequency" }).click();
        await page.getByRole("option", { name: "Daily" }).click();

        await page.getByRole("button", { name: "Select a category" }).click();
        await page.getByRole("option").first().click();

        await page.getByRole("button", { name: "Select a wallet" }).click();
        await page.getByRole("option").first().click();

        await page.getByRole("button", { name: "Create pattern" }).click();
        await expect(page).toHaveURL(/\/recurring$/);

        // Switch to Forecast tab and verify items are shown
        await page.getByRole("tab", { name: "Forecast" }).click();
        await expect(page.getByTestId("forecast-item").first()).toBeVisible();
    });
});
