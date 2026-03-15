package com.kakeibo.features.friends.presentation

import com.kakeibo.features.friends.domain.Friend

sealed interface FriendsUiState {
    data object Loading : FriendsUiState
    data class Success(val friends: List<Friend>) : FriendsUiState
    data class Error(val message: String) : FriendsUiState
}
