package com.kakeibo.features.budgets.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.EditBudgetRoute
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.budgets.domain.Budget
import com.kakeibo.features.budgets.domain.BudgetStatus
import kotlinx.coroutines.flow.collectLatest

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BudgetDetailScreen(
    budgetId: String,
    navController: NavController,
    viewModel: BudgetsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.detailState.collectAsStateWithLifecycle()

    LaunchedEffect(budgetId) { viewModel.loadDetail(budgetId) }
    LaunchedEffect(Unit) {
        viewModel.events.collectLatest { event ->
            if (event is BudgetEvent.Deleted) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Budget Detail") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    if (uiState is BudgetDetailUiState.Success) {
                        IconButton(onClick = { navController.navigate(EditBudgetRoute(budgetId)) }) {
                            Icon(Icons.Filled.Edit, contentDescription = "Edit")
                        }
                        IconButton(onClick = { viewModel.deleteBudget(budgetId) }) {
                            Icon(Icons.Filled.Delete, contentDescription = "Delete")
                        }
                    }
                },
            )
        },
    ) { padding ->
        when (val state = uiState) {
            is BudgetDetailUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is BudgetDetailUiState.Error -> ErrorState(
                message = state.message,
                onRetry = { viewModel.loadDetail(budgetId) },
                modifier = Modifier.padding(padding),
            )
            is BudgetDetailUiState.Success -> BudgetDetailContent(
                budget = state.budget,
                modifier = Modifier.padding(padding),
            )
        }
    }
}

@Composable
private fun BudgetDetailContent(budget: Budget, modifier: Modifier = Modifier) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Card(modifier = Modifier.fillMaxWidth()) {
            Column(modifier = Modifier.padding(20.dp)) {
                Text(
                    text = budget.name,
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    text = budget.categoryName,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(modifier = Modifier.height(16.dp))
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
                        text = "${budget.percentUsed.toInt()}% used",
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Medium,
                    )
                    Text(
                        text = "${CurrencyFormatter.format(budget.currentSpending, budget.currency)} / ${CurrencyFormatter.format(budget.limitAmount, budget.currency)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }
        val remaining = budget.limitAmount - budget.currentSpending
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = if (remaining >= 0) MaterialTheme.colorScheme.secondaryContainer
                else MaterialTheme.colorScheme.errorContainer,
            ),
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = if (remaining >= 0) "Remaining" else "Overspent",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSecondaryContainer,
                )
                Text(
                    text = CurrencyFormatter.format(kotlin.math.abs(remaining), budget.currency),
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                )
            }
        }
    }
}
