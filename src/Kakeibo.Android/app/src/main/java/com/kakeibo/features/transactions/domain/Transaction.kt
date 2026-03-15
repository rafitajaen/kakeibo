package com.kakeibo.features.transactions.domain

import kotlinx.serialization.Serializable

@Serializable
data class Transaction(
    val id: String,
    val walletId: String,
    val walletName: String,
    val type: TransactionType,
    val amount: Double,
    val description: String,
    val categoryId: String,
    val categoryName: String,
    val categoryColor: String?,
    val date: String,
    val notes: String = "",
    val destinationWalletId: String? = null,
    val destinationWalletName: String? = null,
)

@Serializable
enum class TransactionType { INCOME, EXPENSE, TRANSFER }
