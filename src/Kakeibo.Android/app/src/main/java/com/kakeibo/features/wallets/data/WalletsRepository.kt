package com.kakeibo.features.wallets.data

import com.kakeibo.core.api.ApiResult
import com.kakeibo.features.wallets.domain.Wallet
import com.kakeibo.features.wallets.domain.WalletMember

interface WalletsRepository {
    suspend fun getWallets(): ApiResult<List<Wallet>>
    suspend fun getWallet(id: String): ApiResult<Wallet>
    suspend fun createWallet(name: String, type: String, color: String?): ApiResult<Wallet>
    suspend fun updateWallet(id: String, name: String, color: String?): ApiResult<Wallet>
    suspend fun deleteWallet(id: String): ApiResult<Unit>
    suspend fun getMembers(walletId: String): ApiResult<List<WalletMember>>
    suspend fun inviteMember(walletId: String, email: String): ApiResult<Unit>
    suspend fun removeMember(walletId: String, userId: String): ApiResult<Unit>
}
