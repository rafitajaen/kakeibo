package com.kakeibo

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.navigation.compose.rememberNavController
import com.kakeibo.core.api.SessionManager
import com.kakeibo.core.auth.TokenStore
import com.kakeibo.core.navigation.AppNavGraph
import com.kakeibo.core.navigation.LoginRoute
import com.kakeibo.core.theme.KakeiboTheme
import dagger.hilt.android.AndroidEntryPoint
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    @Inject lateinit var sessionManager: SessionManager
    @Inject lateinit var tokenStore: TokenStore

    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        setContent {
            KakeiboTheme {
                val navController = rememberNavController()

                // Navigate to login whenever the session expires (401 received on any request)
                LaunchedEffect(Unit) {
                    sessionManager.tokenExpiredFlow.collect {
                        navController.navigate(LoginRoute) {
                            popUpTo(0) { inclusive = true }
                        }
                    }
                }

                AppNavGraph(
                    navController = navController,
                    startAuthenticated = tokenStore.accessToken != null,
                )
            }
        }
    }
}
