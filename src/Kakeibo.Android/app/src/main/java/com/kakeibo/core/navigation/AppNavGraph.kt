package com.kakeibo.core.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.toRoute
import com.kakeibo.core.navigation.shell.AppShell
import com.kakeibo.features.auth.presentation.ForgotPasswordScreen
import com.kakeibo.features.auth.presentation.LoginScreen
import com.kakeibo.features.auth.presentation.RegisterScreen
import com.kakeibo.features.auth.presentation.ResetPasswordScreen
import com.kakeibo.features.auth.presentation.VerifyEmailScreen
import com.kakeibo.features.onboarding.presentation.OnboardingScreen

/**
 * Root navigation graph for the Kakeibo app.
 *
 * - Auth graph: login, register, email verification, forgot/reset password, onboarding.
 * - Main graph: [AppShell] with bottom nav wrapping all authenticated screens.
 *
 * [startAuthenticated] skips the auth flow when a valid token is already stored.
 */
@Composable
fun AppNavGraph(
    navController: NavHostController = rememberNavController(),
    startAuthenticated: Boolean = false,
) {
    NavHost(
        navController = navController,
        startDestination = if (startAuthenticated) DashboardRoute else LoginRoute,
    ) {
        composable<LoginRoute> {
            LoginScreen(navController = navController)
        }
        composable<RegisterRoute> {
            RegisterScreen(navController = navController)
        }
        composable<VerifyEmailRoute> {
            VerifyEmailScreen(navController = navController)
        }
        composable<ForgotPasswordRoute> {
            ForgotPasswordScreen(navController = navController)
        }
        composable<ResetPasswordRoute> { entry ->
            val route: ResetPasswordRoute = entry.toRoute()
            ResetPasswordScreen(token = route.token, navController = navController)
        }
        composable<OnboardingRoute> {
            OnboardingScreen(navController = navController)
        }

        // Authenticated shell — bottom nav + inner NavHost for all feature screens
        composable<DashboardRoute> {
            AppShell(outerNavController = navController)
        }
    }
}
