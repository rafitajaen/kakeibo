package com.kakeibo.features.activity.presentation

import com.kakeibo.features.activity.domain.ActivityEntry

sealed interface ActivityUiState {
    data object Loading : ActivityUiState
    data class Success(val entries: List<ActivityEntry>) : ActivityUiState
    data class Error(val message: String) : ActivityUiState
}
