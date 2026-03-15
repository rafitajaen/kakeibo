package com.kakeibo.features.dashboard.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowDownward
import androidx.compose.material.icons.filled.ArrowUpward
import androidx.compose.material.icons.filled.Category
import androidx.compose.material.icons.filled.SwapHoriz
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.ui.AmountText
import com.kakeibo.core.ui.AmountType
import com.kakeibo.core.ui.CategoryColors
import com.kakeibo.core.ui.CategoryIcon
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.KakeiboTopBar
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.ui.TransactionRow
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.dashboard.domain.DashboardSummary
import com.kakeibo.features.dashboard.domain.RecentTransaction
import com.kakeibo.features.transactions.domain.TransactionType

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    navController: NavController,
    viewModel: DashboardViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isRefreshing = uiState is DashboardUiState.Loading

    Scaffold(
        topBar = {
            KakeiboTopBar(
                title = "Dashboard",
                onAvatarClick = { /* TODO: navigate to settings */ },
            )
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = isRefreshing,
            onRefresh = viewModel::load,
            modifier = Modifier
                .fillMaxSize()
                .padding(padding),
        ) {
            when (val state = uiState) {
                is DashboardUiState.Loading -> LoadingState()
                is DashboardUiState.Error -> ErrorState(
                    message = state.message,
                    onRetry = viewModel::load,
                )
                is DashboardUiState.Success -> DashboardContent(state.summary)
            }
        }
    }
}

@Composable
private fun DashboardContent(summary: DashboardSummary) {
    LazyColumn(
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        item { MetricsRow(summary) }
        item {
            Text(
                text = "Recent Transactions",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold,
            )
        }
        if (summary.recentTransactions.isEmpty()) {
            item {
                Text(
                    text = "No transactions yet. Start recording your expenses!",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        } else {
            items(summary.recentTransactions) { tx ->
                RecentTransactionItem(tx, summary.currency)
            }
        }
    }
}

@Composable
private fun MetricsRow(summary: DashboardSummary) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        MetricCard(
            label = "Income",
            amount = CurrencyFormatter.format(summary.totalIncome, summary.currency),
            amountType = AmountType.INCOME,
            modifier = Modifier.weight(1f),
        )
        MetricCard(
            label = "Expenses",
            amount = CurrencyFormatter.format(summary.totalExpenses, summary.currency),
            amountType = AmountType.EXPENSE,
            modifier = Modifier.weight(1f),
        )
    }
    Spacer(modifier = Modifier.height(0.dp))
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.primaryContainer,
        ),
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = "Net Savings",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.7f),
            )
            AmountText(
                amount = CurrencyFormatter.format(summary.netSavings, summary.currency),
                type = if (summary.netSavings >= 0) AmountType.INCOME else AmountType.EXPENSE,
                style = MaterialTheme.typography.titleLarge,
            )
        }
    }
}

@Composable
private fun MetricCard(
    label: String,
    amount: String,
    amountType: AmountType,
    modifier: Modifier = Modifier,
) {
    Card(modifier = modifier) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(modifier = Modifier.height(4.dp))
            AmountText(amount = amount, type = amountType, style = MaterialTheme.typography.titleMedium)
        }
    }
}

@Composable
private fun RecentTransactionItem(tx: RecentTransaction, currency: String) {
    val type = when (tx.type.uppercase()) {
        "INCOME" -> TransactionType.INCOME
        "EXPENSE" -> TransactionType.EXPENSE
        else -> TransactionType.TRANSFER
    }
    val amountType = when (type) {
        TransactionType.INCOME -> AmountType.INCOME
        TransactionType.EXPENSE -> AmountType.EXPENSE
        TransactionType.TRANSFER -> AmountType.TRANSFER
    }
    val icon = when (type) {
        TransactionType.INCOME -> Icons.Filled.ArrowUpward
        TransactionType.EXPENSE -> Icons.Filled.ArrowDownward
        TransactionType.TRANSFER -> Icons.Filled.SwapHoriz
    }
    val color = CategoryColors.fromHex(tx.categoryColor)
        .takeIf { it != CategoryColors.Other }
        ?: CategoryColors.forName(tx.categoryName)

    TransactionRow(
        categoryName = tx.categoryName,
        categoryColor = color,
        categoryIcon = icon,
        description = tx.description,
        walletName = tx.walletName,
        amount = CurrencyFormatter.format(tx.amount, currency),
        amountType = amountType,
        onClick = {},
    )
}
