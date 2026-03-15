package com.kakeibo.features.recurring.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.outlined.Repeat
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SuggestionChip
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.CreateRecurringRoute
import com.kakeibo.core.ui.AmountText
import com.kakeibo.core.ui.AmountType
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.recurring.domain.RecurringPattern

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RecurringListScreen(
    navController: NavController,
    viewModel: RecurringViewModel = hiltViewModel(),
) {
    val uiState by viewModel.listState.collectAsStateWithLifecycle()
    val currency = viewModel.userCurrency

    Scaffold(
        topBar = { TopAppBar(title = { Text("Recurring") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(CreateRecurringRoute) }) {
                Icon(Icons.Filled.Add, contentDescription = "Create pattern")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is RecurringListUiState.Loading,
            onRefresh = viewModel::loadPatterns,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is RecurringListUiState.Loading -> LoadingState()
                is RecurringListUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadPatterns)
                is RecurringListUiState.Success -> {
                    if (state.patterns.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.Repeat,
                            title = "No recurring patterns",
                            subtitle = "Automate recurring transactions.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            items(state.patterns) { pattern ->
                                RecurringCard(
                                    pattern = pattern,
                                    currency = currency,
                                    onDelete = { viewModel.deletePattern(pattern.id) },
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
private fun RecurringCard(pattern: RecurringPattern, currency: String, onDelete: () -> Unit) {
    val amountType = when (pattern.type.uppercase()) {
        "INCOME" -> AmountType.INCOME
        "EXPENSE" -> AmountType.EXPENSE
        else -> AmountType.TRANSFER
    }
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = pattern.description,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                )
                Text(
                    text = "${pattern.categoryName} • ${pattern.walletName}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(modifier = Modifier.padding(top = 4.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                    SuggestionChip(
                        onClick = {},
                        label = { Text(pattern.frequency, style = MaterialTheme.typography.labelSmall) },
                    )
                    pattern.nextOccurrence?.let {
                        SuggestionChip(
                            onClick = {},
                            label = { Text("Next: $it", style = MaterialTheme.typography.labelSmall) },
                        )
                    }
                }
            }
            Spacer(modifier = Modifier.width(8.dp))
            AmountText(
                amount = CurrencyFormatter.format(pattern.amount, currency),
                type = amountType,
                style = MaterialTheme.typography.titleSmall,
            )
            IconButton(onClick = onDelete) {
                Icon(
                    Icons.Filled.Delete,
                    contentDescription = "Delete",
                    tint = MaterialTheme.colorScheme.error,
                )
            }
        }
    }
}
