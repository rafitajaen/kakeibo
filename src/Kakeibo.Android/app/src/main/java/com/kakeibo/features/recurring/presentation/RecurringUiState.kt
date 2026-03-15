package com.kakeibo.features.recurring.presentation

import com.kakeibo.features.categories.domain.Category
import com.kakeibo.features.recurring.domain.RecurringPattern
import com.kakeibo.features.wallets.domain.Wallet

sealed interface RecurringListUiState {
    data object Loading : RecurringListUiState
    data class Success(val patterns: List<RecurringPattern>) : RecurringListUiState
    data class Error(val message: String) : RecurringListUiState
}

sealed interface CreatePatternUiState {
    data object Idle : CreatePatternUiState
    data object Loading : CreatePatternUiState
    data class Ready(val wallets: List<Wallet>, val categories: List<Category>) : CreatePatternUiState
    data class Error(val message: String) : CreatePatternUiState
}
