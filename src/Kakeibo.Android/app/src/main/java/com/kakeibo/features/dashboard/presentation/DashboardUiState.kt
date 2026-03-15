package com.kakeibo.features.dashboard.presentation

import com.kakeibo.features.dashboard.domain.DashboardSummary

sealed interface DashboardUiState {
    data object Loading : DashboardUiState
    data class Success(val summary: DashboardSummary) : DashboardUiState
    data class Error(val message: String) : DashboardUiState
}
