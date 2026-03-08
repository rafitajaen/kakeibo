package com.kakeibo.core.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.toRoute
import com.kakeibo.core.navigation.shell.AppShell

/**
 * Root navigation graph for the Kakeibo app.
 *
 * Structure:
 * - Auth graph: login, register, email verification, forgot/reset password
 * - Main graph: app shell (bottom nav / nav rail / drawer) wrapping all authenticated screens
 *
 * Deep links from FCM notifications are handled by passing the intent to [navController].
 */
@Composable
fun AppNavGraph(
    navController: NavHostController = rememberNavController(),
) {
    NavHost(
        navController = navController,
        startDestination = LoginRoute,
    ) {
        // Auth screens (no shell / bottom nav)
        composable<LoginRoute> {
            // TODO: LoginScreen(navController)
        }
        composable<RegisterRoute> {
            // TODO: RegisterScreen(navController)
        }
        composable<VerifyEmailRoute> {
            // TODO: VerifyEmailScreen(navController)
        }
        composable<ForgotPasswordRoute> {
            // TODO: ForgotPasswordScreen(navController)
        }
        composable<ResetPasswordRoute> { entry ->
            val route: ResetPasswordRoute = entry.toRoute()
            // TODO: ResetPasswordScreen(token = route.token, navController)
        }
        composable<OnboardingRoute> {
            // TODO: OnboardingScreen(navController)
        }

        // Main authenticated shell — bottom nav / nav rail / drawer selected automatically
        composable<DashboardRoute> {
            AppShell(navController = navController)
        }
    }
}
