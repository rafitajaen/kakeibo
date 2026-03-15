package com.kakeibo.features.settings.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.core.auth.TokenStore
import com.kakeibo.features.settings.data.ChangePasswordRequest
import com.kakeibo.features.settings.data.SettingsApi
import com.kakeibo.features.settings.data.UpdateProfileRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface SettingsEvent {
    data object LoggedOut : SettingsEvent
    data object ProfileUpdated : SettingsEvent
    data object PasswordChanged : SettingsEvent
    data object AccountDeleted : SettingsEvent
    data class Error(val message: String) : SettingsEvent
}

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val settingsApi: SettingsApi,
    private val tokenStore: TokenStore,
) : ViewModel() {

    private val _uiState = MutableStateFlow<SettingsUiState>(SettingsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _events = Channel<SettingsEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadProfile()
    }

    fun loadProfile() {
        viewModelScope.launch {
            _uiState.value = SettingsUiState.Loading
            when (val result = safeApiCall { settingsApi.getProfile() }) {
                is ApiResult.Success -> _uiState.value = SettingsUiState.Success(result.data)
                is ApiResult.Error -> _uiState.value = SettingsUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = SettingsUiState.Error("Network error.")
            }
        }
    }

    fun updateProfile(username: String) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                settingsApi.updateProfile(UpdateProfileRequest(username))
            }) {
                is ApiResult.Success -> {
                    _uiState.value = SettingsUiState.Success(result.data)
                    _events.send(SettingsEvent.ProfileUpdated)
                }
                is ApiResult.Error -> _events.send(SettingsEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(SettingsEvent.Error("Network error."))
            }
        }
    }

    fun changePassword(currentPassword: String, newPassword: String) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                settingsApi.changePassword(ChangePasswordRequest(currentPassword, newPassword))
            }) {
                is ApiResult.Success -> _events.send(SettingsEvent.PasswordChanged)
                is ApiResult.Error -> _events.send(SettingsEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(SettingsEvent.Error("Network error."))
            }
        }
    }

    fun logout() {
        viewModelScope.launch {
            safeApiCall { settingsApi.logout() }
            tokenStore.clear()
            _events.send(SettingsEvent.LoggedOut)
        }
    }

    fun deleteAccount() {
        viewModelScope.launch {
            when (val result = safeApiCall { settingsApi.deleteAccount() }) {
                is ApiResult.Success -> {
                    tokenStore.clear()
                    _events.send(SettingsEvent.AccountDeleted)
                }
                is ApiResult.Error -> _events.send(SettingsEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(SettingsEvent.Error("Network error."))
            }
        }
    }
}
