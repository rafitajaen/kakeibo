package com.kakeibo.features.transactions.presentation

import com.kakeibo.features.categories.domain.Category
import com.kakeibo.features.transactions.domain.Transaction
import com.kakeibo.features.wallets.domain.Wallet
import kotlinx.datetime.LocalDate

sealed interface TransactionsUiState {
    data object Loading : TransactionsUiState
    data class Success(
        val groupedTransactions: Map<LocalDate, List<Transaction>>,
        val totalIncome: Double,
        val totalExpenses: Double,
        val currency: String,
    ) : TransactionsUiState
    data class Error(val message: String) : TransactionsUiState
}

sealed interface RecordTransactionUiState {
    data object Idle : RecordTransactionUiState
    data object Loading : RecordTransactionUiState
    data class Ready(val wallets: List<Wallet>, val categories: List<Category>) : RecordTransactionUiState
    data class Error(val message: String) : RecordTransactionUiState
}
