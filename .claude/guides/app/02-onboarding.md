# 02 — Onboarding Wizard

## Purpose

The onboarding wizard guides new users through initial app setup immediately after their first login. It is a multi-step flow displayed as a centered card with a progress indicator and navigation controls.

---

## When It Appears

The onboarding wizard is shown automatically to users who have no wallets yet. When a new user logs in for the first time, the app detects the absence of wallets and redirects them to `/onboarding`. Users who already have wallets skip onboarding and go directly to the dashboard.

Users can also be redirected here after clicking "Get started" from a post-registration message.

---

## Structure

The wizard is a single view component (`OnboardingView.vue`) that renders one step at a time based on a `currentStep` integer (0 through 4). A progress indicator at the top shows the current step number out of the total. Navigation buttons at the bottom allow moving forward, backward, or skipping the wizard entirely.

---

## Step 0 — Welcome

The welcome screen introduces Kakeibo. It displays the app logo and tagline: "Mindful money management." A short paragraph explains the app's purpose — helping users track personal and shared finances with intention.

**Actions:**

- **Get Started** — advances to Step 1 (Tour).
- **Skip** — skips the entire wizard and redirects to the dashboard.

---

## Step 1 — Feature Tour

This step shows a brief overview of the platform's four main capabilities. Each feature is presented as a card or list item with an icon and a one-sentence description:

- **Wallets** — organize your money into personal and shared containers.
- **Transactions** — record every income, expense, and transfer.
- **Budgets** — set spending limits and get alerted before you exceed them.
- **Goals** — track savings targets and celebrate milestones.

**Actions:**

- **Back** — returns to Step 0.
- **Next** — advances to Step 2 (Wallet Setup).
- **Skip** — exits the wizard and goes to the dashboard.

---

## Step 2 — Create Your First Wallet

This step prompts the user to create their first personal wallet. It embeds a simplified wallet creation form.

**Fields:**

- **Wallet name** — text input. Required. Max 100 characters. Placeholder suggests examples like "Checking Account" or "Cash."

**Actions:**

- **Back** — returns to Step 1.
- **Create wallet** — submits the wallet creation request to the API. On success, the wallet is created and the wizard advances to Step 3. While submitting, the button shows a loading state.
- **Skip** — skips wallet creation and goes to the dashboard. The user can create wallets later from the Wallets section.

**Error handling:**

- If the wallet name is empty, inline validation prevents submission.
- API errors are shown inline below the form.

---

## Step 3 — Completion

This step confirms that setup is complete. It displays a success illustration or checkmark icon, the message "You're all set!", and a brief note encouraging the user to start recording transactions.

**Actions:**

- **Back** — returns to Step 2 (but the wallet was already created; going back does not undo it).
- **Finish** — advances to Step 4 (Seed Data offer).

---

## Step 4 — Sample Data Offer

This optional final step offers to populate the user's account with sample data so they can explore the app's features without entering real transactions manually.

The screen explains what sample data includes: a set of example wallets, categories, transactions, budgets, and goals spanning a few months.

**Actions:**

- **Yes, load sample data** — triggers a POST request to the seed data endpoint. While loading, the button shows a spinner and is disabled. On success, the wizard closes and the user is redirected to the dashboard, which now shows the sample data.
- **No thanks** — skips sample data loading and redirects to the dashboard immediately.

**Loading state:** The `isSeedLoading` flag disables both buttons and shows a spinner on the "Yes" button while the seed request is in progress.

---

## Navigation Summary

| Step | Title | Back target | Next/primary action |
|------|-------|-------------|---------------------|
| 0 | Welcome | — | Get Started (→ Step 1) or Skip |
| 1 | Feature Tour | Step 0 | Next (→ Step 2) or Skip |
| 2 | Wallet Setup | Step 1 | Create wallet (→ Step 3) or Skip |
| 3 | Completion | Step 2 | Finish (→ Step 4) |
| 4 | Sample Data | Step 3 | Yes / No thanks (→ Dashboard) |

---

## After Onboarding

Once the wizard is complete (or skipped), the `hasCompletedOnboarding` flag is set in the user's profile. Subsequent logins bypass the wizard entirely and go directly to the dashboard.
