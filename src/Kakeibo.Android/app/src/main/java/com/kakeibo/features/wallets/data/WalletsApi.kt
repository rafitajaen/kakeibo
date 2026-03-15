package com.kakeibo.features.wallets.data

import com.kakeibo.features.wallets.domain.Wallet
import com.kakeibo.features.wallets.domain.WalletMember
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface WalletsApi {

    @GET("wallets")
    suspend fun getWallets(): List<Wallet>

    @GET("wallets/{id}")
    suspend fun getWallet(@Path("id") id: String): Wallet

    @POST("wallets")
    suspend fun createWallet(@Body request: CreateWalletRequest): Wallet

    @PUT("wallets/{id}")
    suspend fun updateWallet(@Path("id") id: String, @Body request: UpdateWalletRequest): Wallet

    @DELETE("wallets/{id}")
    suspend fun deleteWallet(@Path("id") id: String)

    @GET("wallets/{id}/members")
    suspend fun getWalletMembers(@Path("id") id: String): List<WalletMember>

    @POST("wallets/{id}/invite")
    suspend fun inviteMember(@Path("id") id: String, @Body request: InviteMemberRequest)

    @DELETE("wallets/{id}/members/{userId}")
    suspend fun removeMember(@Path("id") id: String, @Path("userId") userId: String)
}

@Serializable
data class CreateWalletRequest(
    val name: String,
    val type: String,
    val color: String? = null,
)

@Serializable
data class UpdateWalletRequest(
    val name: String,
    val color: String? = null,
)

@Serializable
data class InviteMemberRequest(val email: String)
