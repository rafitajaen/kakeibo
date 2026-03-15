package com.kakeibo.features.wallets.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

/** Room entity for the wallet offline cache. Source of truth is always the API. */
@Entity(tableName = "wallets")
data class WalletEntity(
    @PrimaryKey val id: String,
    val name: String,
    val type: String,
    val balance: Double,
    val currency: String,
    val isArchived: Boolean,
    val memberCount: Int,
)
