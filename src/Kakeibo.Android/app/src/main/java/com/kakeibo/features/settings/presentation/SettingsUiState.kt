package com.kakeibo.features.settings.presentation

import com.kakeibo.features.auth.domain.User

sealed interface SettingsUiState {
    data object Loading : SettingsUiState
    data class Success(val user: User) : SettingsUiState
    data class Error(val message: String) : SettingsUiState
}
