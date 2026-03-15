package com.kakeibo.features.transactions.presentation

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowDownward
import androidx.compose.material.icons.filled.ArrowUpward
import androidx.compose.material.icons.filled.Receipt
import androidx.compose.material.icons.filled.SwapHoriz
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.Scaffold
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.Text
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.RecordTransactionRoute
import com.kakeibo.core.ui.AmountType
import com.kakeibo.core.ui.CategoryColors
import com.kakeibo.core.ui.DateGroupHeader
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.ui.TransactionRow
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.core.util.DateFormatter
import com.kakeibo.features.transactions.domain.TransactionType

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GlobalTransactionsScreen(
    navController: NavController,
    viewModel: TransactionsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Transactions") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(RecordTransactionRoute("")) }) {
                Icon(Icons.Filled.Add, contentDescription = "Record transaction")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is TransactionsUiState.Loading,
            onRefresh = { viewModel.loadTransactions() },
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is TransactionsUiState.Loading -> LoadingState()
                is TransactionsUiState.Error -> ErrorState(
                    message = state.message,
                    onRetry = { viewModel.loadTransactions() },
                )
                is TransactionsUiState.Success -> {
                    if (state.groupedTransactions.isEmpty()) {
                        EmptyState(
                            icon = Icons.Filled.Receipt,
                            title = "No transactions yet",
                            subtitle = "Tap + to record your first transaction.",
                        )
                    } else {
                        LazyColumn(modifier = Modifier.fillMaxSize()) {
                            state.groupedTransactions.forEach { (date, transactions) ->
                                val subtotal = transactions.sumOf { tx ->
                                    when (tx.type) {
                                        TransactionType.INCOME -> tx.amount
                                        TransactionType.EXPENSE -> -tx.amount
                                        TransactionType.TRANSFER -> 0.0
                                    }
                                }
                                item(key = "header_$date") {
                                    DateGroupHeader(
                                        dateLabel = DateFormatter.formatGroupHeader(date),
                                        subtotal = CurrencyFormatter.format(kotlin.math.abs(subtotal), state.currency),
                                        subtotalType = if (subtotal >= 0) AmountType.INCOME else AmountType.EXPENSE,
                                    )
                                }
                                items(transactions, key = { it.id }) { tx ->
                                    val amountType = when (tx.type) {
                                        TransactionType.INCOME -> AmountType.INCOME
                                        TransactionType.EXPENSE -> AmountType.EXPENSE
                                        TransactionType.TRANSFER -> AmountType.TRANSFER
                                    }
                                    val icon = when (tx.type) {
                                        TransactionType.INCOME -> Icons.Filled.ArrowUpward
                                        TransactionType.EXPENSE -> Icons.Filled.ArrowDownward
                                        TransactionType.TRANSFER -> Icons.Filled.SwapHoriz
                                    }
                                    TransactionRow(
                                        categoryName = tx.categoryName,
                                        categoryColor = CategoryColors.fromHex(tx.categoryColor)
                                            .takeIf { it != CategoryColors.Other }
                                            ?: CategoryColors.forName(tx.categoryName),
                                        categoryIcon = icon,
                                        description = tx.description,
                                        walletName = tx.walletName,
                                        amount = CurrencyFormatter.format(tx.amount, "USD"),
                                        amountType = amountType,
                                        onClick = {},
                                    )
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
