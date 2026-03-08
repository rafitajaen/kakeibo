package com.kakeibo.core.navigation.shell

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.AccountBalanceWallet
import androidx.compose.material.icons.outlined.BarChart
import androidx.compose.material.icons.outlined.Dashboard
import androidx.compose.material.icons.outlined.Notifications
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.material3.adaptive.navigationsuite.NavigationSuiteScaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavDestination.Companion.hasRoute
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.kakeibo.core.navigation.ActivityFeedRoute
import com.kakeibo.core.navigation.DashboardRoute
import com.kakeibo.core.navigation.NotificationListRoute
import com.kakeibo.core.navigation.SettingsRoute
import com.kakeibo.core.navigation.WalletListRoute
import kotlinx.serialization.Serializable

/** Top-level navigation destinations shown in bottom nav / nav rail / drawer. */
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
 * Adaptive app shell.
 *
 * [NavigationSuiteScaffold] automatically selects the correct navigation component
 * based on the current window size class:
 * - Compact  (<600dp)  → BottomNavigationBar
 * - Medium   (600–840dp) → NavigationRail
 * - Expanded (>840dp)  → NavigationDrawer
 */
@Composable
fun AppShell(navController: NavHostController) {
    val innerNavController = rememberNavController()
    val backStackEntry by innerNavController.currentBackStackEntryAsState()
    val currentDestination = backStackEntry?.destination

    NavigationSuiteScaffold(
        navigationSuiteItems = {
            TopLevelDestination.entries.forEach { destination ->
                item(
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
        },
    ) {
        NavHost(
            navController = innerNavController,
            startDestination = DashboardRoute,
        ) {
            composable<DashboardRoute> {
                // TODO: DashboardScreen(navController)
            }
            composable<WalletListRoute> {
                // TODO: WalletListScreen(navController)
            }
            composable<ActivityFeedRoute> {
                // TODO: ActivityFeedScreen(navController)
            }
            composable<NotificationListRoute> {
                // TODO: NotificationListScreen(navController)
            }
            composable<SettingsRoute> {
                // TODO: SettingsScreen(navController)
            }
        }
    }
}
