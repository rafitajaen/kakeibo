package com.kakeibo.core.navigation

import kotlinx.serialization.Serializable

// ─── Auth ────────────────────────────────────────────────────────────────────
@Serializable data object LoginRoute
@Serializable data object RegisterRoute
@Serializable data object VerifyEmailRoute
@Serializable data object ForgotPasswordRoute
@Serializable data class ResetPasswordRoute(val token: String)

// ─── Main shell ──────────────────────────────────────────────────────────────
@Serializable data object DashboardRoute
@Serializable data object OnboardingRoute

// ─── Wallets ─────────────────────────────────────────────────────────────────
@Serializable data object WalletListRoute
@Serializable data class WalletDetailRoute(val walletId: String)
@Serializable data object CreateWalletRoute
@Serializable data class EditWalletRoute(val walletId: String)
@Serializable data class WalletMembersRoute(val walletId: String)
@Serializable data class InviteMemberRoute(val walletId: String)

// ─── Transactions ─────────────────────────────────────────────────────────────
@Serializable data class TransactionListRoute(val walletId: String)
@Serializable data class RecordTransactionRoute(val walletId: String)
@Serializable data class EditTransactionRoute(val transactionId: String, val walletId: String)

// ─── Categories ──────────────────────────────────────────────────────────────
@Serializable data object CategoryListRoute
@Serializable data object CreateCategoryRoute
@Serializable data class EditCategoryRoute(val categoryId: String)

// ─── Budgets ─────────────────────────────────────────────────────────────────
@Serializable data object BudgetListRoute
@Serializable data object CreateBudgetRoute
@Serializable data class EditBudgetRoute(val budgetId: String)
@Serializable data class BudgetDetailRoute(val budgetId: String)

// ─── Goals ───────────────────────────────────────────────────────────────────
@Serializable data object GoalListRoute
@Serializable data object CreateGoalRoute
@Serializable data class EditGoalRoute(val goalId: String)
@Serializable data class GoalDetailRoute(val goalId: String)

// ─── Recurring ───────────────────────────────────────────────────────────────
@Serializable data object RecurringListRoute
@Serializable data object CreateRecurringRoute
@Serializable data class EditRecurringRoute(val patternId: String)

// ─── Notifications ───────────────────────────────────────────────────────────
@Serializable data object NotificationListRoute

// ─── Activity ────────────────────────────────────────────────────────────────
@Serializable data object ActivityFeedRoute

// ─── Friends / Collaboration ─────────────────────────────────────────────────
@Serializable data object FriendListRoute

// ─── Settings ────────────────────────────────────────────────────────────────
@Serializable data object SettingsRoute
@Serializable data object ProfileSettingsRoute
@Serializable data object SecuritySettingsRoute
@Serializable data object NotificationSettingsRoute
@Serializable data object DisplaySettingsRoute
@Serializable data object SessionsRoute

// ─── Admin (super-admin only) ─────────────────────────────────────────────────
@Serializable data object AdminRoute
@Serializable data object AdminUsersRoute
@Serializable data object AdminPlatformPolicyRoute
