package com.kakeibo.features.friends.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.friends.data.FriendRequestBody
import com.kakeibo.features.friends.data.FriendsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface FriendEvent {
    data object RequestSent : FriendEvent
    data object Removed : FriendEvent
    data class Error(val message: String) : FriendEvent
}

@HiltViewModel
class FriendsViewModel @Inject constructor(
    private val friendsApi: FriendsApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<FriendsUiState>(FriendsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _events = Channel<FriendEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadFriends()
    }

    fun loadFriends() {
        viewModelScope.launch {
            _uiState.value = FriendsUiState.Loading
            when (val result = safeApiCall { friendsApi.getFriends() }) {
                is ApiResult.Success -> _uiState.value = FriendsUiState.Success(result.data)
                is ApiResult.Error -> _uiState.value = FriendsUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = FriendsUiState.Error("Network error.")
            }
        }
    }

    fun sendRequest(email: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { friendsApi.sendRequest(FriendRequestBody(email)) }) {
                is ApiResult.Success -> _events.send(FriendEvent.RequestSent)
                is ApiResult.Error -> _events.send(FriendEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(FriendEvent.Error("Network error."))
            }
        }
    }

    fun removeFriend(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { friendsApi.removeFriend(id) }) {
                is ApiResult.Success -> {
                    loadFriends()
                    _events.send(FriendEvent.Removed)
                }
                is ApiResult.Error -> _events.send(FriendEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(FriendEvent.Error("Network error."))
            }
        }
    }
}
