package com.kakeibo.core.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext

// Kakeibo brand green — mirrors the web app's primary colour
private val KakeiboGreen = Color(0xFF1A6B3A)
private val KakeiboGreenLight = Color(0xFF6EDB98)
private val KakeiboGreenContainer = Color(0xFFB7F0CE)
private val KakeiboGreenContainerDark = Color(0xFF005227)

private val LightColorScheme = lightColorScheme(
    primary = KakeiboGreen,
    onPrimary = Color.White,
    primaryContainer = KakeiboGreenContainer,
    onPrimaryContainer = KakeiboGreenContainerDark,
    secondary = Color(0xFF4A8F6B),
    onSecondary = Color.White,
)

private val DarkColorScheme = darkColorScheme(
    primary = KakeiboGreenLight,
    onPrimary = Color(0xFF003919),
    primaryContainer = KakeiboGreenContainerDark,
    onPrimaryContainer = KakeiboGreenContainer,
    secondary = Color(0xFF89CFAA),
    onSecondary = Color(0xFF00391E),
)

/**
 * KakeiboTheme applies Material 3 theming with:
 * - Dynamic Color on Android 12+ (wallpaper-based palette)
 * - Fallback to brand green palette on older devices
 * - Automatic dark/light mode via [isSystemInDarkTheme]
 *
 * @param darkTheme Override dark mode (defaults to system setting)
 * @param dynamicColor Enable Material You dynamic color on Android 12+ (default: true)
 */
@Composable
fun KakeiboTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = true,
    content: @Composable () -> Unit,
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            val context = LocalContext.current
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = KakeiboTypography,
        content = content,
    )
}
