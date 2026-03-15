package com.kakeibo.features.categories.presentation

import com.kakeibo.features.categories.domain.Category

sealed interface CategoryListUiState {
    data object Loading : CategoryListUiState
    data class Success(val categories: List<Category>) : CategoryListUiState
    data class Error(val message: String) : CategoryListUiState
}
