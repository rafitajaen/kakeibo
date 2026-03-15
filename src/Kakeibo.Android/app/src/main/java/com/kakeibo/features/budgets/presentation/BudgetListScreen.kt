package com.kakeibo.features.budgets.presentation

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
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.outlined.PieChart
import androidx.compose.material3.Badge
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.BudgetDetailRoute
import com.kakeibo.core.navigation.CreateBudgetRoute
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.budgets.domain.Budget
import com.kakeibo.features.budgets.domain.BudgetStatus

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BudgetListScreen(
    navController: NavController,
    viewModel: BudgetsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.listState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Budgets") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(CreateBudgetRoute) }) {
                Icon(Icons.Filled.Add, contentDescription = "Create budget")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is BudgetListUiState.Loading,
            onRefresh = viewModel::loadBudgets,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is BudgetListUiState.Loading -> LoadingState()
                is BudgetListUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadBudgets)
                is BudgetListUiState.Success -> {
                    if (state.budgets.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.PieChart,
                            title = "No budgets yet",
                            subtitle = "Create a budget to monitor your spending.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            items(state.budgets) { budget ->
                                BudgetCard(
                                    budget = budget,
                                    onClick = { navController.navigate(BudgetDetailRoute(budget.id)) },
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun BudgetCard(budget: Budget, onClick: () -> Unit) {
    Card(onClick = onClick, modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = budget.name,
                        style = MaterialTheme.typography.titleSmall,
                        fontWeight = FontWeight.SemiBold,
                    )
                    Text(
                        text = budget.categoryName,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                StatusBadge(budget.status)
            }
            Spacer(modifier = Modifier.height(12.dp))
            LinearProgressIndicator(
                progress = { (budget.percentUsed / 100.0).toFloat().coerceIn(0f, 1f) },
                modifier = Modifier.fillMaxWidth(),
                color = when (budget.status) {
                    BudgetStatus.ON_TRACK -> MaterialTheme.colorScheme.primary
                    BudgetStatus.WARNING -> Color(0xFFF59E0B)
                    BudgetStatus.EXCEEDED -> MaterialTheme.colorScheme.error
                },
            )
            Spacer(modifier = Modifier.height(8.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Text(
                    text = CurrencyFormatter.format(budget.currentSpending, budget.currency),
                    style = MaterialTheme.typography.bodySmall,
                )
                Text(
                    text = "of ${CurrencyFormatter.format(budget.limitAmount, budget.currency)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}

@Composable
private fun StatusBadge(status: BudgetStatus) {
    val (label, color) = when (status) {
        BudgetStatus.ON_TRACK -> "On Track" to MaterialTheme.colorScheme.primaryContainer
        BudgetStatus.WARNING -> "Warning" to Color(0xFFFEF3C7)
        BudgetStatus.EXCEEDED -> "Exceeded" to MaterialTheme.colorScheme.errorContainer
    }
    Badge(containerColor = color) {
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            modifier = Modifier.padding(horizontal = 4.dp),
        )
    }
}
