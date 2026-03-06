// Hierarchical breadcrumb definition for each named route.
// Each entry defines the chain of breadcrumb segments leading to that route.
// Segments with `dynamic: true` resolve their label from the router params + stores at runtime.

export interface BreadcrumbSegment {
    // i18n key for the label (used when dynamic is false)
    labelKey: string;
    // Named route to link to (undefined = current page, non-linkable)
    routeName?: string;
    // Whether this segment's label is resolved dynamically at runtime
    dynamic?: boolean;
}

// Map from route name → ordered list of segments (from root to current)
export const BREADCRUMB_MAP: Record<string, BreadcrumbSegment[]> = {
    home: [{ labelKey: "dashboard.title" }],

    // Wallets
    wallets: [{ labelKey: "wallets.title" }],
    "wallets-create": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "wallets.createNew" },
    ],
    "wallet-detail": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true }, // resolved to wallet name
    ],
    "wallet-edit": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "wallets.actions.edit" },
    ],
    "wallet-members": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "wallets.members.title" },
    ],
    "wallet-invite": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "wallets.invitation.title" },
    ],

    // Transactions
    transactions: [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "transactions.title" },
    ],
    "transaction-record": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "transactions.title", routeName: "transactions" },
        { labelKey: "transactions.createNew" },
    ],
    "transaction-edit": [
        { labelKey: "wallets.title", routeName: "wallets" },
        { labelKey: "", dynamic: true },
        { labelKey: "transactions.title", routeName: "transactions" },
        { labelKey: "wallets.form.submitEdit" },
    ],

    // Categories
    categories: [{ labelKey: "categories.title" }],
    "category-create": [
        { labelKey: "categories.title", routeName: "categories" },
        { labelKey: "categories.createNew" },
    ],
    "category-edit": [
        { labelKey: "categories.title", routeName: "categories" },
        { labelKey: "categories.actions.edit" },
    ],

    // Budgets
    budgets: [{ labelKey: "app.budgets" }],
    "budget-create": [
        { labelKey: "app.budgets", routeName: "budgets" },
        { labelKey: "wallets.budgets.createNew" },
    ],
    "budget-edit": [
        { labelKey: "app.budgets", routeName: "budgets" },
        { labelKey: "wallets.budgets.actions.edit" },
    ],

    // Goals
    goals: [{ labelKey: "app.goals" }],
    "goal-create": [
        { labelKey: "app.goals", routeName: "goals" },
        { labelKey: "wallets.goals.createNew" },
    ],
    "goal-edit": [
        { labelKey: "app.goals", routeName: "goals" },
        { labelKey: "wallets.goals.actions.edit" },
    ],

    // Recurring
    recurring: [{ labelKey: "app.recurring" }],
    "recurring-create": [
        { labelKey: "app.recurring", routeName: "recurring" },
        { labelKey: "wallets.recurring.createNew" },
    ],
    "recurring-edit": [
        { labelKey: "app.recurring", routeName: "recurring" },
        { labelKey: "wallets.recurring.actions.edit" },
    ],

    // Other
    "notification-preferences": [{ labelKey: "notifications.prefs.title" }],
    activity: [{ labelKey: "activity.title" }],
    settings: [{ labelKey: "settings.title" }],
};
