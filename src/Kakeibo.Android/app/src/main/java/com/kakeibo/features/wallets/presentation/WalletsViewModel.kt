package com.kakeibo.features.wallets.presentation

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.navigation.toRoute
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.navigation.WalletDetailRoute
import com.kakeibo.features.wallets.data.WalletsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface WalletEvent {
    data object Saved : WalletEvent
    data object Deleted : WalletEvent
    data class Error(val message: String) : WalletEvent
}

@HiltViewModel
class WalletsViewModel @Inject constructor(
    private val repository: WalletsRepository,
    savedStateHandle: SavedStateHandle,
) : ViewModel() {

    private val walletId: String? = runCatching {
        savedStateHandle.toRoute<WalletDetailRoute>().walletId
    }.getOrNull()

    private val _listState = MutableStateFlow<WalletListUiState>(WalletListUiState.Loading)
    val listState = _listState.asStateFlow()

    private val _detailState = MutableStateFlow<WalletDetailUiState>(WalletDetailUiState.Loading)
    val detailState = _detailState.asStateFlow()

    private val _events = Channel<WalletEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadWallets()
        if (walletId != null) loadDetail(walletId)
    }

    fun loadWallets() {
        viewModelScope.launch {
            _listState.value = WalletListUiState.Loading
            when (val result = repository.getWallets()) {
                is ApiResult.Success -> _listState.value = WalletListUiState.Success(result.data)
                is ApiResult.Error -> _listState.value = WalletListUiState.Error(result.message)
                is ApiResult.NetworkError -> _listState.value = WalletListUiState.Error("Network error.")
            }
        }
    }

    fun loadDetail(id: String) {
        viewModelScope.launch {
            _detailState.value = WalletDetailUiState.Loading
            val walletResult = repository.getWallet(id)
            val membersResult = repository.getMembers(id)
            if (walletResult is ApiResult.Success && membersResult is ApiResult.Success) {
                _detailState.value = WalletDetailUiState.Success(
                    wallet = walletResult.data,
                    members = membersResult.data,
                )
            } else {
                _detailState.value = WalletDetailUiState.Error("Failed to load wallet.")
            }
        }
    }

    fun createWallet(name: String, type: String, color: String?) {
        viewModelScope.launch {
            when (val result = repository.createWallet(name, type, color)) {
                is ApiResult.Success -> {
                    loadWallets()
                    _events.send(WalletEvent.Saved)
                }
                is ApiResult.Error -> _events.send(WalletEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(WalletEvent.Error("Network error."))
            }
        }
    }

    fun deleteWallet(id: String) {
        viewModelScope.launch {
            when (val result = repository.deleteWallet(id)) {
                is ApiResult.Success -> {
                    loadWallets()
                    _events.send(WalletEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(WalletEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(WalletEvent.Error("Network error."))
            }
        }
    }

    fun inviteMember(walletId: String, email: String) {
        viewModelScope.launch {
            when (val result = repository.inviteMember(walletId, email)) {
                is ApiResult.Success -> {
                    loadDetail(walletId)
                    _events.send(WalletEvent.Saved)
                }
                is ApiResult.Error -> _events.send(WalletEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(WalletEvent.Error("Network error."))
            }
        }
    }
}
