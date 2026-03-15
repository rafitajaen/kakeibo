package com.kakeibo.features.budgets.presentation

import com.kakeibo.features.budgets.domain.Budget
import com.kakeibo.features.categories.domain.Category

sealed interface BudgetListUiState {
    data object Loading : BudgetListUiState
    data class Success(val budgets: List<Budget>) : BudgetListUiState
    data class Error(val message: String) : BudgetListUiState
}

sealed interface BudgetDetailUiState {
    data object Loading : BudgetDetailUiState
    data class Success(val budget: Budget) : BudgetDetailUiState
    data class Error(val message: String) : BudgetDetailUiState
}

sealed interface CreateBudgetUiState {
    data object Idle : CreateBudgetUiState
    data object Loading : CreateBudgetUiState
    data class Ready(val categories: List<Category>) : CreateBudgetUiState
    data class Error(val message: String) : CreateBudgetUiState
}
