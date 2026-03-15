package com.kakeibo.features.wallets.data

import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.wallets.domain.Wallet
import com.kakeibo.features.wallets.domain.WalletMember
import javax.inject.Inject

class WalletsRepositoryImpl @Inject constructor(
    private val api: WalletsApi,
) : WalletsRepository {

    override suspend fun getWallets(): ApiResult<List<Wallet>> = safeApiCall { api.getWallets() }
    override suspend fun getWallet(id: String): ApiResult<Wallet> = safeApiCall { api.getWallet(id) }

    override suspend fun createWallet(name: String, type: String, color: String?): ApiResult<Wallet> =
        safeApiCall { api.createWallet(CreateWalletRequest(name, type, color)) }

    override suspend fun updateWallet(id: String, name: String, color: String?): ApiResult<Wallet> =
        safeApiCall { api.updateWallet(id, UpdateWalletRequest(name, color)) }

    override suspend fun deleteWallet(id: String): ApiResult<Unit> =
        safeApiCall { api.deleteWallet(id) }

    override suspend fun getMembers(walletId: String): ApiResult<List<WalletMember>> =
        safeApiCall { api.getWalletMembers(walletId) }

    override suspend fun inviteMember(walletId: String, email: String): ApiResult<Unit> =
        safeApiCall { api.inviteMember(walletId, InviteMemberRequest(email)) }

    override suspend fun removeMember(walletId: String, userId: String): ApiResult<Unit> =
        safeApiCall { api.removeMember(walletId, userId) }
}
