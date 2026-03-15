package com.kakeibo.features.onboarding.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.AccountBalanceWallet
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.TrendingUp
import androidx.compose.material3.Button
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.navigation.NavController
import com.kakeibo.core.navigation.DashboardRoute

private data class OnboardingPage(
    val icon: ImageVector,
    val title: String,
    val subtitle: String,
)

private val pages = listOf(
    OnboardingPage(
        icon = Icons.Outlined.AccountBalanceWallet,
        title = "Track Your Finances",
        subtitle = "Record every transaction to gain complete visibility into your money.",
    ),
    OnboardingPage(
        icon = Icons.Outlined.TrendingUp,
        title = "Set Goals & Budgets",
        subtitle = "Create budgets to control spending and goals to build wealth.",
    ),
    OnboardingPage(
        icon = Icons.Outlined.CheckCircle,
        title = "You're All Set!",
        subtitle = "Start your Kakeibo journey. Conscious spending leads to financial freedom.",
    ),
)

@Composable
fun OnboardingScreen(navController: NavController) {
    var currentPage by remember { mutableIntStateOf(0) }
    val page = pages[currentPage]

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Icon(
            imageVector = page.icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(96.dp),
        )
        Spacer(modifier = Modifier.height(32.dp))
        Text(
            text = page.title,
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center,
        )
        Spacer(modifier = Modifier.height(12.dp))
        Text(
            text = page.subtitle,
            style = MaterialTheme.typography.bodyLarge,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
        )
        Spacer(modifier = Modifier.height(64.dp))

        if (currentPage < pages.lastIndex) {
            Button(
                onClick = { currentPage++ },
                modifier = Modifier.fillMaxWidth().height(52.dp),
            ) { Text("Next") }
            Spacer(modifier = Modifier.height(12.dp))
            OutlinedButton(
                onClick = {
                    navController.navigate(DashboardRoute) { popUpTo(0) { inclusive = true } }
                },
                modifier = Modifier.fillMaxWidth(),
            ) { Text("Skip") }
        } else {
            Button(
                onClick = {
                    navController.navigate(DashboardRoute) { popUpTo(0) { inclusive = true } }
                },
                modifier = Modifier.fillMaxWidth().height(52.dp),
            ) { Text("Get Started") }
        }

        Spacer(modifier = Modifier.height(32.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            pages.forEachIndexed { idx, _ ->
                val color = if (idx == currentPage) {
                    MaterialTheme.colorScheme.primary
                } else {
                    MaterialTheme.colorScheme.outlineVariant
                }
                Icon(
                    imageVector = Icons.Outlined.CheckCircle,
                    contentDescription = null,
                    tint = color,
                    modifier = Modifier.size(12.dp),
                )
            }
        }
    }
}
