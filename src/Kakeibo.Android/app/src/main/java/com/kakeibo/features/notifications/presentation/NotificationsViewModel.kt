package com.kakeibo.features.notifications.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.notifications.data.NotificationsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class NotificationsViewModel @Inject constructor(
    private val notificationsApi: NotificationsApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<NotificationsUiState>(NotificationsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init {
        loadNotifications()
    }

    fun loadNotifications() {
        viewModelScope.launch {
            _uiState.value = NotificationsUiState.Loading
            when (val result = safeApiCall { notificationsApi.getNotifications() }) {
                is ApiResult.Success -> {
                    val notifications = result.data
                    _uiState.value = NotificationsUiState.Success(
                        notifications = notifications,
                        unreadCount = notifications.count { !it.isRead },
                    )
                }
                is ApiResult.Error -> _uiState.value = NotificationsUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = NotificationsUiState.Error("Network error.")
            }
        }
    }

    fun markAsRead(id: String) {
        viewModelScope.launch {
            safeApiCall { notificationsApi.markAsRead(id) }
            loadNotifications()
        }
    }

    fun markAllAsRead() {
        viewModelScope.launch {
            safeApiCall { notificationsApi.markAllAsRead() }
            loadNotifications()
        }
    }
}
