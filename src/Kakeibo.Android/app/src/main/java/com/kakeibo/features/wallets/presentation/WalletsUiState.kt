package com.kakeibo.features.wallets.presentation

import com.kakeibo.features.wallets.domain.Wallet
import com.kakeibo.features.wallets.domain.WalletMember

sealed interface WalletListUiState {
    data object Loading : WalletListUiState
    data class Success(val wallets: List<Wallet>) : WalletListUiState
    data class Error(val message: String) : WalletListUiState
}

sealed interface WalletDetailUiState {
    data object Loading : WalletDetailUiState
    data class Success(val wallet: Wallet, val members: List<WalletMember>) : WalletDetailUiState
    data class Error(val message: String) : WalletDetailUiState
}
