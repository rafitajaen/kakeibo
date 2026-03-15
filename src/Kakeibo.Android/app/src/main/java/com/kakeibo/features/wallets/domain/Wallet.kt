package com.kakeibo.features.wallets.domain

import kotlinx.serialization.Serializable

@Serializable
data class Wallet(
    val id: String,
    val name: String,
    val type: WalletType,
    val color: String?,
    val balance: Double,
    val currency: String,
    val isArchived: Boolean,
    val memberCount: Int = 1,
)

@Serializable
enum class WalletType { PERSONAL, SHARED }

@Serializable
data class WalletMember(
    val userId: String,
    val username: String,
    val email: String,
    val joinedAt: String,
)
