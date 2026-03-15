package com.kakeibo.features.transactions.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

/** Room entity for the transaction offline cache. Source of truth is always the API. */
@Entity(tableName = "transactions")
data class TransactionEntity(
    @PrimaryKey val id: String,
    val walletId: String,
    val type: String,
    val amount: Double,
    val description: String,
    val categoryId: String,
    val categoryName: String,
    val date: String,
    val notes: String?,
    val destinationWalletId: String?,
)
