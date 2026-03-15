package com.kakeibo.core.navigation.shell

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.AccountBalanceWallet
import androidx.compose.material.icons.outlined.BarChart
import androidx.compose.material.icons.outlined.Dashboard
import androidx.compose.material.icons.outlined.Notifications
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.foundation.layout.padding
import androidx.navigation.NavDestination.Companion.hasRoute
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.toRoute
import com.kakeibo.core.navigation.ActivityFeedRoute
import com.kakeibo.core.navigation.BudgetDetailRoute
import com.kakeibo.core.navigation.BudgetListRoute
import com.kakeibo.core.navigation.CategoryListRoute
import com.kakeibo.core.navigation.CreateBudgetRoute
import com.kakeibo.core.navigation.CreateCategoryRoute
import com.kakeibo.core.navigation.CreateGoalRoute
import com.kakeibo.core.navigation.CreateRecurringRoute
import com.kakeibo.core.navigation.CreateWalletRoute
import com.kakeibo.core.navigation.DashboardRoute
import com.kakeibo.core.navigation.EditBudgetRoute
import com.kakeibo.core.navigation.EditCategoryRoute
import com.kakeibo.core.navigation.EditGoalRoute
import com.kakeibo.core.navigation.EditRecurringRoute
import com.kakeibo.core.navigation.FriendListRoute
import com.kakeibo.core.navigation.GoalDetailRoute
import com.kakeibo.core.navigation.GoalListRoute
import com.kakeibo.core.navigation.InviteMemberRoute
import com.kakeibo.core.navigation.NotificationListRoute
import com.kakeibo.core.navigation.RecordTransactionRoute
import com.kakeibo.core.navigation.RecurringListRoute
import com.kakeibo.core.navigation.SettingsRoute
import com.kakeibo.core.navigation.WalletDetailRoute
import com.kakeibo.core.navigation.WalletListRoute
import com.kakeibo.features.activity.presentation.ActivityFeedScreen
import com.kakeibo.features.budgets.presentation.BudgetDetailScreen
import com.kakeibo.features.budgets.presentation.BudgetListScreen
import com.kakeibo.features.budgets.presentation.CreateBudgetScreen
import com.kakeibo.features.budgets.presentation.EditBudgetScreen
import com.kakeibo.features.categories.presentation.CategoryListScreen
import com.kakeibo.features.categories.presentation.CreateCategoryScreen
import com.kakeibo.features.categories.presentation.EditCategoryScreen
import com.kakeibo.features.dashboard.presentation.DashboardScreen
import com.kakeibo.features.friends.presentation.FriendListScreen
import com.kakeibo.features.goals.presentation.CreateGoalScreen
import com.kakeibo.features.goals.presentation.EditGoalScreen
import com.kakeibo.features.goals.presentation.GoalDetailScreen
import com.kakeibo.features.goals.presentation.GoalListScreen
import com.kakeibo.features.notifications.presentation.NotificationListScreen
import com.kakeibo.features.recurring.presentation.CreatePatternScreen
import com.kakeibo.features.recurring.presentation.EditPatternScreen
import com.kakeibo.features.recurring.presentation.RecurringListScreen
import com.kakeibo.features.settings.presentation.SettingsScreen
import com.kakeibo.features.transactions.presentation.GlobalTransactionsScreen
import com.kakeibo.features.transactions.presentation.RecordTransactionScreen
import com.kakeibo.features.wallets.presentation.CreateWalletScreen
import com.kakeibo.features.wallets.presentation.InviteMemberScreen
import com.kakeibo.features.wallets.presentation.WalletDetailScreen
import com.kakeibo.features.wallets.presentation.WalletListScreen

/** Top-level navigation destinations shown in bottom nav. */
enum class TopLevelDestination(
    val icon: ImageVector,
    val label: String,
    val route: Any,
) {
    Dashboard(Icons.Outlined.Dashboard, "Dashboard", DashboardRoute),
    Wallets(Icons.Outlined.AccountBalanceWallet, "Wallets", WalletListRoute),
    Activity(Icons.Outlined.BarChart, "Activity", ActivityFeedRoute),
    Notifications(Icons.Outlined.Notifications, "Notifications", NotificationListRoute),
    Settings(Icons.Outlined.Settings, "Settings", SettingsRoute),
}

/**
 * App shell with bottom navigation bar and inner NavHost hosting all authenticated screens.
 *
 * [outerNavController] is the root NavController used only for auth-level navigation (e.g. logout).
 */
@Composable
fun AppShell(outerNavController: NavHostController) {
    val innerNavController = rememberNavController()
    val backStackEntry by innerNavController.currentBackStackEntryAsState()
    val currentDestination = backStackEntry?.destination

    Scaffold(
        bottomBar = {
            NavigationBar {
                TopLevelDestination.entries.forEach { destination ->
                    NavigationBarItem(
                        icon = { Icon(destination.icon, contentDescription = null) },
                        label = { Text(destination.label) },
                        selected = currentDestination?.hasRoute(destination.route::class) == true,
                        onClick = {
                            innerNavController.navigate(destination.route) {
                                popUpTo(innerNavController.graph.startDestinationId) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        },
                    )
                }
            }
        },
    ) { innerPadding ->
        NavHost(
            navController = innerNavController,
            startDestination = DashboardRoute,
            modifier = Modifier.padding(innerPadding),
        ) {
            // ── Bottom nav top-level destinations ──────────────────────────
            composable<DashboardRoute> {
                DashboardScreen(navController = innerNavController)
            }
            composable<WalletListRoute> {
                WalletListScreen(navController = innerNavController)
            }
            composable<ActivityFeedRoute> {
                ActivityFeedScreen(navController = innerNavController)
            }
            composable<NotificationListRoute> {
                NotificationListScreen(navController = innerNavController)
            }
            composable<SettingsRoute> {
                SettingsScreen(
                    navController = innerNavController,
                    onLogout = {
                        outerNavController.navigate(com.kakeibo.core.navigation.LoginRoute) {
                            popUpTo(0) { inclusive = true }
                        }
                    },
                )
            }

            // ── Wallets ────────────────────────────────────────────────────
            composable<WalletDetailRoute> { entry ->
                val route: WalletDetailRoute = entry.toRoute()
                WalletDetailScreen(walletId = route.walletId, navController = innerNavController)
            }
            composable<CreateWalletRoute> {
                CreateWalletScreen(navController = innerNavController)
            }
            composable<InviteMemberRoute> { entry ->
                val route: InviteMemberRoute = entry.toRoute()
                InviteMemberScreen(walletId = route.walletId, navController = innerNavController)
            }

            // ── Transactions ───────────────────────────────────────────────
            composable<RecordTransactionRoute> { entry ->
                val route: RecordTransactionRoute = entry.toRoute()
                RecordTransactionScreen(
                    preselectedWalletId = route.walletId.takeIf { it.isNotBlank() },
                    navController = innerNavController,
                )
            }

            // ── Categories ─────────────────────────────────────────────────
            composable<CategoryListRoute> {
                CategoryListScreen(navController = innerNavController)
            }
            composable<CreateCategoryRoute> {
                CreateCategoryScreen(navController = innerNavController)
            }
            composable<EditCategoryRoute> { entry ->
                val route: EditCategoryRoute = entry.toRoute()
                EditCategoryScreen(categoryId = route.categoryId, navController = innerNavController)
            }

            // ── Budgets ────────────────────────────────────────────────────
            composable<BudgetListRoute> {
                BudgetListScreen(navController = innerNavController)
            }
            composable<CreateBudgetRoute> {
                CreateBudgetScreen(navController = innerNavController)
            }
            composable<EditBudgetRoute> { entry ->
                val route: EditBudgetRoute = entry.toRoute()
                EditBudgetScreen(budgetId = route.budgetId, navController = innerNavController)
            }
            composable<BudgetDetailRoute> { entry ->
                val route: BudgetDetailRoute = entry.toRoute()
                BudgetDetailScreen(budgetId = route.budgetId, navController = innerNavController)
            }

            // ── Goals ──────────────────────────────────────────────────────
            composable<GoalListRoute> {
                GoalListScreen(navController = innerNavController)
            }
            composable<CreateGoalRoute> {
                CreateGoalScreen(navController = innerNavController)
            }
            composable<EditGoalRoute> { entry ->
                val route: EditGoalRoute = entry.toRoute()
                EditGoalScreen(goalId = route.goalId, navController = innerNavController)
            }
            composable<GoalDetailRoute> { entry ->
                val route: GoalDetailRoute = entry.toRoute()
                GoalDetailScreen(goalId = route.goalId, navController = innerNavController)
            }

            // ── Recurring ──────────────────────────────────────────────────
            composable<RecurringListRoute> {
                RecurringListScreen(navController = innerNavController)
            }
            composable<CreateRecurringRoute> {
                CreatePatternScreen(navController = innerNavController)
            }
            composable<EditRecurringRoute> { entry ->
                val route: EditRecurringRoute = entry.toRoute()
                EditPatternScreen(patternId = route.patternId, navController = innerNavController)
            }

            // ── Friends ────────────────────────────────────────────────────
            composable<FriendListRoute> {
                FriendListScreen(navController = innerNavController)
            }
        }
    }
}
