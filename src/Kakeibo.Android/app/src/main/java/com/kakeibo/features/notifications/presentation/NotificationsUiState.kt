package com.kakeibo.features.notifications.presentation

import com.kakeibo.features.notifications.domain.AppNotification

sealed interface NotificationsUiState {
    data object Loading : NotificationsUiState
    data class Success(
        val notifications: List<AppNotification>,
        val unreadCount: Int,
    ) : NotificationsUiState
    data class Error(val message: String) : NotificationsUiState
}
